
open System 
open System.Text 
open System.Net 
open System.IO 
open System.Web
open System.Data
open System.Data.Linq
open Microsoft.FSharp.Data.TypeProviders
open Microsoft.FSharp.Linq
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open System.Configuration

type dbSchema = SqlDataConnection<"Data Source=localhost\sql2012;Initial Catalog=RabbitMQTrace;Integrated Security=SSPI;">

let dbConn = ConfigurationManager.AppSettings.["DBConn"]
let db = dbSchema.GetDataContext(dbConn)   

let RabbitUser = ConfigurationManager.AppSettings.["RabbitUser"].ToString()
let RabbitPass = ConfigurationManager.AppSettings.["RabbitPass"].ToString()
let RabbitServer = ConfigurationManager.AppSettings.["RabbitServer"].ToString()
let RabbitManagementPort = ConfigurationManager.AppSettings.["RabbitManagementPort"].ToString()

let Rabbit64Auth = Convert.ToBase64String(Encoding.ASCII.GetBytes(RabbitUser + ":" + RabbitPass))

[<EntryPoint>]
let main argv = 

    let url : String = 
        "http://" + RabbitServer + ":" + RabbitManagementPort + "/api/overview?msg_rates_age=60&msg_rates_incr=5"
    let wc = new WebClient()
    wc.Headers.Add("User-Agent: RabbitStats")
    wc.Headers.Add("Host: " + RabbitServer + ":" + RabbitManagementPort)
    wc.Headers.Add("WWW-Authenticate: Basic realm='RabbitMQ Management'")
    wc.Headers.Add("Authorization: Basic " + Rabbit64Auth)
    
    let st = wc.OpenRead(url)
    let sr = new StreamReader(st)
    let res = sr.ReadToEnd()
    sr.Close()

    let output = JObject.Parse res

    let getlist_samples jsonToken = 
        output.SelectToken(jsonToken).Values<JObject>() 
        |> List.ofSeq
        |> List.map (fun (x: JObject) -> 
            x.SelectToken("timestamp").Value<int64>(), x.SelectToken("sample").Value<int64>())

    let ratecalc pt t ps s = 
      if pt <> 0L then
        //multiply output by 1000 as timestamp in ms
        ((decimal s - decimal ps) / (decimal t - decimal pt)) * decimal 1000
      else decimal 0

    let third (x,y,z) = z

    let nullable value = new System.Nullable<_>(value)

    let sample_rows samples sampleType = 
      samples
      |> List.fold (fun (pts, ps, results) (ts, smp) -> 
        let rate = ratecalc pts ts ps smp
        ts, smp, 
        new dbSchema.ServiceTypes.RabbitStats_Samples(
                          SampleType = sampleType, 
                          Timestamp = nullable ts,
                          Sample = nullable smp,
                          Rate = nullable rate) :: results) (0L, 0L, [])
      |> third
    
    //Add samples to rabbitstats_samples
    [("message_stats.ack_details.samples", "ack")
     ("message_stats.deliver_get_details.samples", "deliver_get")
     ("message_stats.deliver_details.samples", "deliver")
     ("message_stats.publish_details.samples", "publish")
     ("message_stats.redeliver_details.samples", "redeliver")]
    |> List.iter (fun (a,b) ->
      db.RabbitStats_Samples.InsertAllOnSubmit(sample_rows (getlist_samples a) b))

    //Get totals from API JSON document
    let newRecord = new dbSchema.ServiceTypes.RabbitStats(
                                          ServerURL = url,
                                          DateTime = nullable System.DateTime.UtcNow,
                                          PublishRate = nullable (output.SelectToken("message_stats.publish_details.rate").Value<decimal>()),
                                          DeliverRate = nullable (output.SelectToken("message_stats.deliver_details.rate").Value<decimal>()),
                                          AckRate = nullable (output.SelectToken("message_stats.ack_details.rate").Value<decimal>()),
                                          DeliverGetRate = nullable (output.SelectToken("message_stats.deliver_get_details.rate").Value<decimal>()),
                                          RedeliverRate = nullable (output.SelectToken("message_stats.redeliver_details.rate").Value<decimal>()),
                                          Channels = nullable (output.SelectToken("object_totals.channels").Value<int>()),
                                          Connections = nullable (output.SelectToken("object_totals.connections").Value<int>()),
                                          Consumers = nullable (output.SelectToken("object_totals.consumers").Value<int>()),
                                          Exchanges = nullable (output.SelectToken("object_totals.exchanges").Value<int>()),
                                          Queues = nullable (output.SelectToken("object_totals.queues").Value<int>())
                                          )
    
    //Add totals to RabbitStats table
    db.RabbitStats.InsertOnSubmit(newRecord)

    
    try
      db.DataContext.SubmitChanges()
      printfn "Successfully inserted new rows."
    with
      | exn -> printfn "Exception:\n%s" exn.Message

    Console.ReadLine () |> ignore
    0

    

