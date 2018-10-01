namespace agentlib

module EAgent = 
  
  open FSharp.Core
  open Microsoft.FSharp.Control
  open System

  type AfterError<'state> =
    | Continue of 'state
    | TerminateProcess
    | RestartProcess

  (* Agent OTP'ish semantics, abstracting the general loop bits *)
  type MailboxProcessor<'a> with
    static member public SpawnAgent<'b>(messageHandler: 'a ->'b->'b, initialState: 'b, ?afterTimeout: 'b-> int,
                                        ?afterTimeoutHandler: 'b -> AfterError<'b>, ?errorHandler:Exception -> 'a option -> 'b -> AfterError<'b>) : MailboxProcessor<'a> =
        let after = defaultArg afterTimeout (fun _ -> -1)
        let afterHandler = defaultArg afterTimeoutHandler (fun state -> Continue(state))
        let errorHandler = defaultArg errorHandler (fun _ _ state -> Continue(state))
        MailboxProcessor.Start(fun inbox ->
           let rec loop(state) = async{
               let! msg = inbox.TryReceive(after(state))
               try
                    match msg with
                    | None   -> match afterHandler state with
                                | Continue(newState) -> return! loop(newState)
                                | TerminateProcess -> return ()
                                | RestartProcess -> return! loop(initialState)
                    | Some(m) -> return! loop(messageHandler m state)             
               with
               | ex ->  match errorHandler ex msg state with
                        | Continue(newState) -> return! loop(newState)
                        | TerminateProcess -> return ()
                        | RestartProcess -> return! loop(initialState)
           }
           loop(initialState))


  type Control<'msg, 'state> = 
  | Restart
  | Terminate
  | SetName of string
  | SetAgentHandler of ('msg -> 'state -> 'state)
  | SetTimeoutHandler of int * ('state -> AfterError<'state>)
  and UC<'msg, 'state> =
  | User of 'msg
  | Control of Control<'msg, 'state>
  and AsyncAgent<'msg, 'state>(messageHandler:'msg -> 'state -> 'state) =
     let mutable messageHandler = messageHandler
     let mutable afterTimeout = -1
     let mutable afterTimeoutHandler = fun(state:'state) -> Continue(state)
     let mutable mailbox:MailboxProcessor<UC<'msg, 'state>> = Unchecked.defaultof<MailboxProcessor<UC<'msg,'state>>>
     let mutable currentState:'state = Unchecked.defaultof<'state>
     let mutable name = "Undefined"
     let mutable index = 0
    /// Sends a msg to the Agent for processing
     member self.Send msg = mailbox.Post (User(msg))
    /// Sends an internal control msg to the Agent. i.e. Restart, Terminate, SetName("name-here"), etc.
     member self.AgentSend msg = mailbox.Post (Control(msg))
     member internal self.Index with get() = index and set(i) = index <- i
     member internal self.InitializeMailbox(mbox) = mailbox <- mbox
     member internal self.ReplaceState(state) = currentState <- state
     member internal self.MessageHandler with get() = messageHandler and set(f) = messageHandler <- f
     member internal self.AfterTimeout with get() = afterTimeout and set(i) = afterTimeout <- i
     member internal self.AfterTimeoutHandler with get(): 'state -> AfterError<'state> = afterTimeoutHandler and set(f) = afterTimeoutHandler <- f
     member internal self.Mailbox with get() = mailbox and set(m) = mailbox <- m
     member internal self.Name with get() = name and set(n) = name <- n

  exception ControlException  

  let printDefaultError name ex msg state initialState =
    eprintf "The exception below occurred on agent %s at state %A with message %A. The agent was started with state %A.\n%A" name state msg initialState ex
  
  let rec createAgentMailbox (agent: AsyncAgent<'msg, 'state>) initialState = 
    MailboxProcessor<UC<'msg,'state>>.SpawnAgent(
        (fun msg state ->
            agent.ReplaceState(state)
            match msg with
            | Control(c) -> 
                match c with
                | SetName(c) -> agent.Name <- c; state
                | SetAgentHandler(f)  -> 
                    agent.MessageHandler <- f
                    state
                | SetTimeoutHandler(aftertout, afterhandler) ->
                    agent.AfterTimeout <- aftertout
                    agent.AfterTimeoutHandler <- afterhandler; state
                | _ -> raise(ControlException)
            | User(uMsg) ->
                agent.MessageHandler uMsg state),
        initialState,
        afterTimeout = (fun _ -> agent.AfterTimeout),
        afterTimeoutHandler = (fun state -> agent.AfterTimeoutHandler state),
        errorHandler = fun ex msg state ->
                          if msg.IsNone then
                            printDefaultError agent.Name ex Unchecked.defaultof<'msg> state initialState
                            Continue(state)
                          else
                            let m = msg.Value
                            match(m) with
                            | Control(c) ->  
                                match(c) with
                                | Restart -> RestartProcess
                                | Terminate -> TerminateProcess
                                | _ -> Continue(state)
                            | User(_msg) ->
                                printDefaultError agent.Name ex Unchecked.defaultof<'msg> state initialState
                                Continue(state)
    )
  
  (*

    Ex. Counter Process
    -----------------------------

    let counter = spawnAgent (fun msg state -> printfn "current-state: %i" (state + msg); state + msg) 0
    counter.Send 1

    counter.AgentSend(SetAgentHandler(fun m s -> printfn "the state is now: %i" (s + m); s + m ))
    counter.Send 2

  *)
  let spawnAgent (f:'a -> 'b -> 'b) initialState = 
    let agent = new AsyncAgent<'a,'b>(f)
    let mbox = createAgentMailbox agent initialState
    agent.InitializeMailbox mbox
    agent