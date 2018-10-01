namespace Api

open agentlib.EAgent

type AngelNumberAgent() =
    let counter = MailboxProcessor.SpawnAgent((fun msg n ->
                    match msg with
                    | Increment m ->  
                        let newState = n + m
                        printfn " incrementing counter from %i to %i" n newState; newState
                    | StopIPA -> raise(IPAStopException)
                    | Fetch replyChannel ->
                        do replyChannel.Reply(n)
                        n
                  ), 0, errorHandler = (fun _ _ _ -> Continue(0)))
    member self.Increment(n) = counter.Post(Increment(n))
    member self.Stop() = counter.Post(StopIPA)
    member self.Fetch() = counter.PostAndReply(fun replyChannel -> Fetch(replyChannel)) 