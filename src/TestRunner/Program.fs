module runner.Main
open System
open NUnit.Framework
open FsUnit
   
let [<Test>]``Compiler to lambda test``() =                  
    let ec = EdgeCompiler()
    let parameters = dict ["typeName", "EdgeFs.Startup" :> obj
                           "methodName", "Invoke" :> obj                          
                           "source", """fun input -> async{return ".NET welcomes " + input.ToString()}""" :> obj]
    let norman = ec.CompileFunc parameters
    let result = norman.Invoke("JavaScript via F#")
    result.Wait()
    result.Result :?> String |> should equal ".NET welcomes JavaScript via F#"
    
let [<Test>] ``Compiler to .fs file test``() =
    let ec = EdgeCompiler()    
    let parameters = dict ["typeName", "EdgeFs.Startup" :> obj
                           "methodName", "Invoke" :> obj                          
                           "source", "/Users/dave/code/compilertesting/runner/addSeven.fs" :> obj]
    let norman = ec.CompileFunc parameters
    let result = norman.Invoke(3)
    result.Wait()
    result.Result :?> int |> should equal 10
                                   
[<EntryPoint>]
let main args = 

    do ``Compiler to lambda test``()
    do ``Compiler to .fs file test``()
    
    Console.ReadLine() |> ignore
    0

