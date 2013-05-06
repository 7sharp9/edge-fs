namespace global
open System
open System.Collections.Generic
open System.IO
open System.Reflection
open System.Text
open System.Text.RegularExpressions
open System.Threading.Tasks
open System.Security
open System.Security.Permissions
open Microsoft.FSharp.Build
open Microsoft.Build.Framework
open Microsoft.Build.Tasks
open Microsoft.Build.Utilities
 
type EdgeCompiler() =

    let referencesRegex = new Regex(@"\/\/\#r\s+""[^""]+""\s*", RegexOptions.Multiline)
    let referenceRegex = new Regex(@"\/\/\#r\s+""([^""]+)""\s*")
    let debuggingEnabled = not <| String.IsNullOrEmpty(Environment.GetEnvironmentVariable("EDGE_FS_DEBUG"))
    
    let writeSourceToDisk source =
        let path = Path.GetTempPath()
        let fileName = Path.GetRandomFileName() 
        let fileNameEx = Path.ChangeExtension(fileName, ".fs")
        let fullName = Path.Combine(path, fileNameEx)
        File.WriteAllText(fullName, source)
        fullName
      
    let tryCompile(source, references) =
        let fileName = writeSourceToDisk source

        let (emptyBuildEngine, errors) = 
           let errors = StringBuilder()
           { new IBuildEngine3 with
               member b.BuildProjectFilesInParallel(projectFileNames, targetNames, globalProperties, removeGlobalProperties, toolsVersion, returnTargetOutputs) = BuildEngineResult()
               member b.Reacquire() = ()
               member b.Yield() = ()
               member b.BuildProjectFile(projectFileName, targetNames, globalProperties, targetOutputs, toolsVersion) = true
               member b.BuildProjectFilesInParallel(projectFileNames, targetNames, globalProperties, targetOutputsPerProject, toolsVersion, useResultsCache, unloadProjectsOnCompletion) = false
               member b.IsRunningMultipleNodes with get() = true
               member b.BuildProjectFile(projectFileName, targetNames, globalProperties, targetOutputs) = true
               member b.ColumnNumberOfTaskNode with get() = 1
               member b.ContinueOnError with get() = true
               member b.LineNumberOfTaskNode with get() = 0
               member b.LogCustomEvent(e) =()
               member b.LogErrorEvent(e) = errors.AppendLine(e.Message ) |> ignore
               member b.LogMessageEvent(e) =()
               member b.LogWarningEvent(e) =()
               member b.ProjectFileOfTaskNode with get() = String.Empty}, errors
             
        let fsc = Fsc(BuildEngine = emptyBuildEngine,
                      Sources = [|Microsoft.Build.Utilities.TaskItem(fileName)|],
                      References = (references |> Seq.map (fun x -> TaskItem(x:String) :> ITaskItem) |> Seq.toArray), 
                      TargetType = "library",
                      NoFramework = true,
                      OutputAssembly = Path.ChangeExtension(fileName, "dll"),
                      Optimize = not debuggingEnabled,
                      Tailcalls = not debuggingEnabled)
        let assembly = 
            if fsc.Execute() = false then None else
                use fs = new FileStream(fsc.OutputAssembly, FileMode.Open, FileAccess.Read, FileShare.Delete)
                let count = int32 fs.Length 
                if (count <= 0) then None else
                    let buffer = Array.zeroCreate count
                    fs.Read(buffer, 0, count) |> ignore
                    (SecurityPermission(SecurityPermissionFlag.ControlEvidence)).Assert()
                    try Some(Assembly.Load(buffer))
                    finally CodeAccessPermission.RevertAssert()
                            File.Delete fileName
                            File.Delete fsc.OutputAssembly
        fsc.ExitCode, errors.ToString(), assembly
        
    let getReferences (parameters:IDictionary<string, Object>) source=
        // add assembly references provided explicitly through parameters
        let passed = match parameters.TryGetValue("references")  with
                     | true, v -> seq {for item in unbox v do yield unbox item}
                     | _ -> Seq.empty

        // add assembly references provided in code as //#r "assemblyname" comments
        let enc = seq{ for m in referencesRegex.Matches(source) do
                           let referenceMatch = referenceRegex.Match(m.Value)
                           if referenceMatch.Success then
                               yield referenceMatch.Groups.[1].Value }
        seq{ yield! passed
             yield! enc}

    let getLineDirective (parameters:IDictionary<string, Object>) fileName= 
        if (debuggingEnabled) then
            let file = match parameters.TryGetValue("jsFileName") with
                       | true, (:? String as jsFileName) -> jsFileName
                       | _ -> fileName
                       
            let lineNumber = match parameters.TryGetValue("jsLineNumber") with
                             | true, (:? int as number) -> number
                             | _ -> 0
            if String.IsNullOrEmpty(file) then String.Empty
            else String.Format("#line {0} \"{1}\"\n", lineNumber, fileName)
        else String.Empty

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
                invalidOp <| "Unable to compile F# code.\n----> Errors when compiling as a CLR async lambda expression:\n" + errors
                         
        else let result, errors, assembly = tryCompile(lineDirective + source, references)
             if result = 0 then assembly 
             else invalidOp <| "Unable to compile F# code.\n----> Errors when compiling as a CLR library:\n" + errors
          
    member x.CompileFunc( parameters: IDictionary<string, Object>) =
        // read source from file
        let source, isLambda, fileName = 
            match parameters.["source"] :?> String with
            | input when input.EndsWith(".fs", StringComparison.InvariantCultureIgnoreCase) 
                         || input.EndsWith(".fsx", StringComparison.InvariantCultureIgnoreCase)
                -> File.ReadAllText(input), false, input
            | input -> input, true, String.Empty
        
        let references = getReferences parameters source
        let lineDirective = getLineDirective parameters fileName
            
        // extract the entry point to a class method
        match tryGetAssembly lineDirective source references isLambda with
        | Some assembly -> let startupType = assembly.GetType( parameters.["typeName"] :?> String, true, true)
                           let instance = Activator.CreateInstance(startupType, false)
                           match startupType.GetMethod(parameters.["methodName"] :?> String, BindingFlags.Instance ||| BindingFlags.Public) with
                           | null -> invalidOp "Unable to access CLR method to wrap via reflection. Make sure it is a public instance method."
                           | invokeMethod -> // create a Func<object,Task<object>> around the method invocation using reflection
                                             Func<_,_> (fun (input:obj) -> (invokeMethod.Invoke(instance, [| input |])) :?> Task<obj> )
        | None -> invalidOp "Unable to build Dynamic Assembly, no assembly output."