
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
open System.Threading 

type dbSchema = SqlDataConnection<"Data Source=localhost\sql2012;Initial Catalog=RabbitMQTrace;Integrated Security=SSPI;">

let dbConn = ConfigurationManager.AppSettings.["DBConn"]
let db = dbSchema.GetDataContext(dbConn)   

let RabbitUser = ConfigurationManager.AppSettings.["RabbitUser"].ToString()
let RabbitPass = ConfigurationManager.AppSettings.["RabbitPass"].ToString()
let RabbitServer = ConfigurationManager.AppSettings.["RabbitServer"].ToString()
let RabbitManagementPort = ConfigurationManager.AppSettings.["RabbitManagementPort"].ToString()

let Rabbit64Auth = Convert.ToBase64String(Encoding.ASCII.GetBytes(RabbitUser + ":" + RabbitPass))

let RabbitAPIStringOverview = ConfigurationManager.AppSettings.["RabbitAPIStringOverview"].ToString()
let RabbitAPIStringVhosts = ConfigurationManager.AppSettings.["RabbitAPIStringVhosts"].ToString()

let url : String = 
    "http://" + RabbitServer + ":" + RabbitManagementPort + RabbitAPIStringOverview

let nullable value =
    System.Nullable<_>(value)


[<EntryPoint>]
let main argv = 

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

    //Get totals from API JSON document
    let newRecord = new dbSchema.ServiceTypes.RabbitStats(
                                                          ServerURL = url,
                                                          Vhost = "Overview",
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


    Thread.Sleep(5000)
    0
