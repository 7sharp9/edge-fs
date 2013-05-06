namespace global
//#r "System.Core"
open System.Threading.Tasks
type Startup() =
    let addSeven v =  
        v + 7
    member x.Invoke(input:obj) =
        let v = input :?> int
        async.Return (addSeven v :> obj) |> Async.StartAsTask
