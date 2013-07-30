// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.
open System 
open System.Text 
open System.Net 
open System.IO 
open System.Web
open Newtonsoft.Json
open Newtonsoft.Json.Linq

[<EntryPoint>]
let main argv = 
    let url : string = 
        "http://15below-rabmq01.inetuhosted.net:15672/api/overview?msg_rates_age=60&msg_rates_incr=5"
    let wc = new WebClient()
    wc.Headers.Add("User-Agent: RabbitStats")
    wc.Headers.Add("Host: 15below-rabmq01.inetuhosted.net:15672")
    wc.Headers.Add("WWW-Authenticate: Basic realm='RabbitMQ Management'")
    wc.Headers.Add("Authorization: Basic Z3Vlc3Q6Z3Vlc3Q=")
    
    let st = wc.OpenRead(url)
    let sr = new StreamReader(st)
    let res = sr.ReadToEnd()
    sr.Close()

    let output = JObject.Parse res

    let samples = 
        output.SelectToken("message_stats.ack_details.samples").Values<JObject>() 
        |> List.ofSeq
        |> List.map (fun (x: JObject) -> 
            x.SelectToken("timestamp").Value<double>(), x.SelectToken("sample").Value<int>())
    
    Console.ReadLine () |> ignore
    0
    

