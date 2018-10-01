// Learn more about F# at http://fsharp.org

open System
open agentlib
open Api
open Suave
open Suave.Operators

[<EntryPoint>]
let main _argv =
    // startup processes
    let counter = AngelNumberAgent()
    counter.Increment(1)
    counter.Increment(5)
    let app =
        choose
            [
                Suave.Filters.path "/v1/healthcheck" >=> Suave.Filters.GET >=> Successful.OK ""
                Suave.Filters.path "/v1/counter" >=> Suave.Filters.GET >=> Successful.OK (counter.Fetch() |> string)
                RequestErrors.NOT_FOUND "Not Found"
            ] 

    startWebServer { defaultConfig with hideHeader=true; bindings = [HttpBinding.create HTTP (System.Net.IPAddress.Parse("0.0.0.0")) (Sockets.Port.Parse "3456")] } app
    0 // return an integer exit code
