// Learn more about F# at http://fsharp.org

open System
open agentlib
open Api
open Suave
open Suave.Operators
open Suave

[<EntryPoint>]
let main _argv =
    // startup processes
    let angelNumbers = AngelNumberAgent()
    
    let app =
        choose
            [
                Suave.Filters.path "/v1/healthcheck" >=> Suave.Filters.GET >=> Successful.OK ""
                Suave.Filters.pathScan "/v1/number/%d" (fun num -> Successful.OK ((angelNumbers.Fetch(num)) |> fun x -> String.Format("{0}",x.Info)))
                RequestErrors.NOT_FOUND "Not Found"
            ] 

    startWebServer { defaultConfig with hideHeader=true; bindings = [HttpBinding.create HTTP (System.Net.IPAddress.Parse("0.0.0.0")) (Sockets.Port.Parse "3456")] } app
    0 // return an integer exit code
