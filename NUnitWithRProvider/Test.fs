module TestWithRProvider

open System
open RDotNet
open RProvider
open RProvider.stats
open NUnit.Framework

let single = function
   | NumericVector (v) -> v.[0]
   | _ -> failwith "not a numeric vector"

// The test will succeed.
// However, the NUnit test runner won't terminate correctly.
[<Test>]
let ``run nunit test with RProvider`` () = 
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
   match R.ks_test (z, "pnorm") with
      | List (testResult) -> Assert.That (single testResult.["p.value"], Is.GreaterThan (0.05))
      | _ -> Assert.Fail ()
