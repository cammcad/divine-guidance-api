// Learn more about F# at http://fsharp.org

open System
open System.IO
open LiteDB

type AngelNumber() =
  member val Id = 0 with get,set
  member val Number = 0 with get,set
  member val Info = String.Empty with get,set

[<EntryPoint>]
let main argv =
    use db = new LiteDatabase(@"AngelNumber.db")
    let numbers = db.GetCollection<AngelNumber>("numbers")

    Directory.GetFiles("angelnumbers/all")
    |> Seq.iter(fun (f: string) ->
                  let fname = Path.GetFileName f 
                  let fname_parts = fname.Split("-")
                  match Array.length(fname_parts) = 3 with
                  | false -> ()
                  | true ->
                    try
                      let angel_numberinfo = File.ReadAllText f
                      let anum = new AngelNumber()
                      anum.Number <- (int fname_parts.[2])
                      anum.Info <- angel_numberinfo
                      numbers.Insert(anum) |> ignore
                    with e -> printfn "failed on angel number: %s" fname_parts.[2]; printfn "%A" e)
    0 // return an integer exit code
