#r "packages/FAKE/tools/FakeLib.dll"
#r "System.Net.Http.dll"

#I @".\packages\Microsoft.Azure.Management.ResourceManager.Fluent\lib\net452"
#r "Microsoft.Azure.Management.ResourceManager.Fluent.dll"

#I @".\packages\Newtonsoft.Json\lib\net45"
#r "Newtonsoft.Json.dll"

#load @".\paket-files\CompositionalIT\fshelpers\src\FsHelpers\ArmHelper\ArmHelper.fs"

open Cit.Helpers.Arm
open Fake
open Fake.EnvironmentHelper
open Fake.TraceHelper
open System
open System.IO
open System.Net.Http
open System.Text

Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
let mutable azure = None

Target "AzureAuthentication" <| fun _ ->
    tracefn "Authenticating with Azure..."
    let azureCredentials =
        { ClientId = getBuildParam "ClientId" |> Guid
          ClientSecret = getBuildParam "ClientSecret"
          TenantId = getBuildParam "TenantId" |> Guid }
    let subscriptionId = getBuildParam "SubscriptionId" |> Guid
    azure <- Some(authenticate azureCredentials subscriptionId)

Target "CreateResourceGroup" <| fun _ ->
    let resourceGroup = getBuildParam "ResourceGroup"
    let armTemplate = getBuildParam "ArmTemplate"
    
    let a = Parameters.ArmString (getBuildParam "StorageAccountName")
    let p = "storageAccountName", a

    let outputs =
        let deployment =       
            tracefn "Deploying template '%s' to resource group '%s'..." armTemplate resourceGroup
            { DeploymentName =
                getBuildParamOrDefault "BUILD_BUILDNUMBER" (sprintf "LOCAL-%s" (getMachineEnvironment()).MachineName)
                |> fun buildNumber -> buildNumber.Replace(" ", "_")
              ResourceGroup = Existing resourceGroup
              ArmTemplate = File.ReadAllText armTemplate
              Parameters = Parameters.Simple [ p ]
              DeploymentMode = Incremental }

        deployment
        |> deployWithProgress azure.Value
        |> Seq.choose(function
        | DeploymentInProgress (state, operations) -> tracefn "State is %s, completed %d operations." state operations; None
        | DeploymentError (statusCode, message) -> traceError <| sprintf "DEPLOYMENT ERROR: %s - '%s'" statusCode message; None
        | DeploymentCompleted outputs -> Some outputs)
        |> Seq.head

    let storageAccountKey = outputs.TryFind "storageAccountKey"
    tracefn "Done! Storage account key is %A" storageAccountKey

// Build order
"AzureAuthentication"
    ==> "CreateResourceGroup"
    
RunTargetOrDefault "CreateResourceGroup"
