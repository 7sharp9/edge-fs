module runner.Main
open System
open System.Diagnostics
open NUnit.Framework
open FsUnit
   
let [<Test>]``Compiler to lambda test``() =                  
    let ec = EdgeCompiler()
    let parameters = dict ["typeName", "Startup" :> obj
                           "methodName", "Invoke" :> obj                          
                           "source", """fun input -> async{return ".NET welcomes " + input.ToString()}""" :> obj]
    let norman = ec.CompileFunc parameters
    let result = norman.Invoke("JavaScript via F#")
    result.Wait()
    result.Result :?> String |> should equal ".NET welcomes JavaScript via F#"
    
let [<Test>] ``Compiler to .fs file test``() =
    let ec = EdgeCompiler()    
    let parameters = dict ["typeName", "Startup" :> obj
                           "methodName", "Invoke" :> obj                          
                           "source", "../../addSeven.fs" :> obj]
    let norman = ec.CompileFunc parameters
    let result = norman.Invoke(3)
    result.Wait()
    result.Result :?> int |> should equal 10
    
type Stopwatch with member x.StartWithReset = x.Reset >> x.Start
                    member x.StopAndPrint() = x.Stop(); printfn "%fms" x.Elapsed.TotalMilliseconds
                            
[<EntryPoint>]
let main args = 
    
    
    let sw = Stopwatch.StartNew()
    
    do ``Compiler to lambda test``()
    sw.StopAndPrint()
    
    sw.StartWithReset()
    do ``Compiler to .fs file test``()
    sw.StopAndPrint()
    
    Console.ReadLine() |> ignore
    0

