module TestWithRDotNet

open System
open RDotNet
open NUnit.Framework

let engine =
   match Microsoft.Win32.Registry.LocalMachine.OpenSubKey (@"SOFTWARE\R-core") with
   | null -> System.ApplicationException("Registry key is not found.") |> raise
   | rCore ->
      let is64bit = System.Environment.Is64BitProcess
      match rCore.OpenSubKey (if is64bit then "R64" else "R") with
      | null -> System.ApplicationException("Registry key is not found.") |> raise
      | r ->
         let getString key = r.GetValue (key) :?> string
         let (%%) dir name = System.IO.Path.Combine (dir, name) 
         let currentVersion = System.Version (getString "Current Version")
         let binPath = getString "InstallPath" %% "bin"
         if currentVersion < System.Version (2, 12) then
            binPath
         else
            binPath %% if is64bit then "x64" else "i386"
   |> fun path -> System.Environment.SetEnvironmentVariable ("PATH", path)
   let engine = REngine.CreateInstance ("REngine")
   engine.Initialize ()
   engine

let single = function
   | NumericVector (v) -> v.[0]
   | _ -> failwith "not a numeric vector"

[<Test>]
let ``run nunit test without RProvider`` () = 
   let z =
      let random = Random (42)
      let rec boxMuller () = seq {
         let r = sqrt <| -2.0 * log (random.NextDouble ())
         let theta = 2.0 * Math.PI * (random.NextDouble ())
         yield r * cos theta
         yield r * sin theta
         yield! boxMuller ()
      }
      boxMuller () |> Seq.take 1000 |> Seq.toArray
   engine.SetSymbol ("z", engine.CreateNumericVector (z))
   match engine.Evaluate ("""ks.test(z, "pnorm")""") with
      | List (testResult) -> Assert.That (single testResult.["p.value"], Is.GreaterThan (0.05))
      | _ -> Assert.Fail ()
