module runner.Main
open System
open System.Diagnostics
open NUnit.Framework
open FsUnit
   
let [<Test>]``Compiler with lambda test``() =                  
    let ec = EdgeCompiler()
    let parameters = dict ["typeName", "Startup" :> obj
                           "methodName", "Invoke" :> obj                          
                           "source", """fun input -> async{return ".NET welcomes " + input.ToString()}""" :> obj]
    let norman = ec.CompileFunc parameters
    let result = norman.Invoke("JavaScript via F#")
    result.Wait()
    result.Result :?> String |> should equal ".NET welcomes JavaScript via F#"
    
let [<Test>] ``Compiler with .fs file test``() =
    let ec = EdgeCompiler()    
    let parameters = dict ["typeName", "Startup" :> obj
                           "methodName", "Invoke" :> obj                          
                           "source", "../../addSeven.fs" :> obj]
    let norman = ec.CompileFunc parameters
    let result = norman.Invoke(3)
    result.Wait()
    result.Result :?> int |> should equal 10
    
type Stopwatch with member x.StartWithReset = x.Reset >> x.Start
                    member x.StopAndPrint(title) = x.Stop()
                                                   Console.WriteLine( "{0}: {1}ms", title, x.Elapsed.TotalMilliseconds)
                            
[<EntryPoint>]
let main args = 

    let sw = Stopwatch.StartNew()
    
    for i in 1..2 do
        ``Compiler with lambda test``()
        sw.StopAndPrint("Compiler with lambda")

        sw.StartWithReset()
        ``Compiler with .fs file test``()
        sw.StopAndPrint("Compiler with .fs file")
    
    Console.ReadLine() |> ignore
    0

