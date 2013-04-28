namespace global
open System
open System.Text
open System.Collections.Generic
open System.IO
open System.Reflection
open System.Text
open System.Text.RegularExpressions
open System.Threading.Tasks
open Microsoft.FSharp.Compiler.SimpleSourceCodeServices

type EdgeCompiler() =

    let referencesRegex = new Regex(@"\/\/\#r\s+""[^""]+""\s*", RegexOptions.Multiline)
    let referenceRegex = new Regex(@"\/\/\#r\s+""([^""]+)""\s*")
    
    let debuggingEnabled = not <| String.IsNullOrEmpty(Environment.GetEnvironmentVariable("EDGE_FS_DEBUG"))
    
    let writeSourceToDisk source =
        let path = System.IO.Path.GetTempPath()
        let fileName = Path.GetRandomFileName() 
        let fileNameEx = Path.ChangeExtension(fileName, ".fs")
        let fullName = Path.Combine(path, fileNameEx)
        File.WriteAllText(fullName, source)
        fullName
        
    let tryCompile(source, references: List<String>) =
        let fileName = writeSourceToDisk source
        let sscs = SimpleSourceCodeServices()
        let errors, result, ass =
            let parameters = [| yield![| "--use-incremental-build"
                                         "--nologo"
                                         "--target:library"
                                         sprintf "-o:%s"  <| Path.ChangeExtension(fileName, "dll")
                                         "--noframework" |]
                                if debuggingEnabled then yield! [|"--define:DEBUG";"--debug+"|]
                                yield! [|"--optimize-"
                                         "--tailcalls-" |]

                                if references.Count > 0 then yield! references |> Seq.map ((+) "-r:")
                                yield fileName |]
                                                                                            
            sscs.CompileToDynamicAssembly(parameters, None)
        File.Delete fileName
        result, errors, ass
        
    let getReferences (parameters:IDictionary<string, Object>) source=
        let references = ResizeArray()
        // add assembly references provided explicitly through parameters
        match parameters.TryGetValue("references")  with
        | true, v -> for item in v :?> Object[] do references.Add(string item)
        | _ -> ()

        // add assembly references provided in code as //#r "assemblyname" comments
        for m in referencesRegex.Matches(source) do
            let referenceMatch = referenceRegex.Match(m.Value)
            if referenceMatch.Success then
                references.Add(referenceMatch.Groups.[1].Value)
        references

    let getLineDirective (parameters:IDictionary<string, Object>) fileName= 
        if (debuggingEnabled) then
            let file = match parameters.TryGetValue("jsFileName") with
                       | true, (:? String as jsFileName) -> jsFileName
                       | _ -> fileName
                       
            let lineNumber = match parameters.TryGetValue("jsLineNumber") with
                             | true, (:? int as number) -> number
                             | _ -> 0
            if String.IsNullOrEmpty(file) then ""
            else String.Format("#line {0} \"{1}\"\n", lineNumber, fileName)
        else ""
                      
    member x.CompileFunc( parameters: IDictionary<string, Object>) =

        // read source from file
        let source, isLambda, fileName = 
            match parameters.["source"] :?> String with
            | input when input.EndsWith(".fs", StringComparison.InvariantCultureIgnoreCase) 
                         || input.EndsWith(".fsx", StringComparison.InvariantCultureIgnoreCase)
                -> File.ReadAllText(input), false, input
            | input -> input, true, ""
        
        let references = getReferences parameters source
        let lineDirective = getLineDirective parameters fileName
                
        let foldErrors (errorInfo: Microsoft.FSharp.Compiler.ErrorInfo[]) = 
            errorInfo 
            |> Array.fold (fun (sb:StringBuilder) error -> sb.AppendLine(error.ToString()) ) (StringBuilder())
            |> fun sb -> sb.ToString()
            
        let tryGetAssembly lineDirective source references islambda =
            // try to compile source code as a library
            if islambda then         
                let lsource = "namespace global\n"
                              + "open System\n"
                              + "type Startup() =\n"
                              + "    member x.Invoke(input: obj) =\n"
                              + lineDirective
                              + "        async {let! result = input |> (" + source + ")\n"
                              + "               return result :> obj } |> Async.StartAsTask"

                let result, errors, assembly = tryCompile(lsource, references)
                if result = 0 then assembly else
                    invalidOp <| "Unable to compile F# code.\n----> Errors when compiling as a CLR async lambda expression:\n" + foldErrors errors
                             
            else let result, errors, assembly = tryCompile(lineDirective + source, references)
                 if result = 0 then assembly 
                 else invalidOp <| "Unable to compile F# code.\n----> Errors when compiling as a CLR library:\n" + foldErrors errors

        // extract the entry point to a class method
        match tryGetAssembly lineDirective source references isLambda with
        | Some assembly -> let types = assembly.GetTypes()
                           let startupType = assembly.GetType( parameters.["typeName"] :?> String, true, true)
                           let instance = Activator.CreateInstance(startupType, false)
                      
                           match startupType.GetMethod(parameters.["methodName"] :?> String, BindingFlags.Instance ||| BindingFlags.Public) with
                           | null -> invalidOp "Unable to access CLR method to wrap via reflection. Make sure it is a public instance method."
                           | invokeMethod -> // create a Func<object,Task<object>> delegate around the method invocation using reflection
                                             Func<_,_> (fun (input:obj) -> (invokeMethod.Invoke(instance, [| input |])) :?> Task<obj> )
        | None -> invalidOp "Unable to build Dynamic Assembly, no assembly output."

