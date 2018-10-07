namespace Api

open LiteDB
open agentlib.EAgent

type AngelNumberAgent() =
    let counter = MailboxProcessor.SpawnAgent((fun msg n ->
                    match msg with
                    | Fetch(number,replyChannel) ->
                        use db = new LiteDatabase(@"../data/AngelNumber.db")
                        let numbers = db.GetCollection<AngelNumber>("numbers")
                        let angelNumber = numbers.FindOne(fun x -> x.Number = number)
                        do replyChannel.Reply(angelNumber)
                        n
                    | StopAngelNumberAgent -> raise(AngelNumberReqStopException)
                  ), (AngelNumber()), errorHandler = (fun ex msg _ ->
                                                         printfn "[Error] Agent-AngelNumber received msg: %A \n Exception: %A" msg ex
                                                         match ex with
                                                         | AngelNumberReqStopException -> TerminateProcess
                                                         | _ -> Continue (AngelNumber())))
    member self.Fetch(x) = counter.PostAndReply(fun replyChannel -> Fetch(x,replyChannel))