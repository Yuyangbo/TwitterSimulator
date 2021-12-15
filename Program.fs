namespace TwitterWeb
open System
open System.Collections.Generic
open System.Security.Cryptography
open System.Text
open Newtonsoft.Json
open Akka.Actor
open Akka.FSharp
open Suave
open Suave.Sockets
open Suave.Sockets.Control
open Suave.WebSocket
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.Logging
open Suave.Writers
open MessageType

module TwitterWeb =
    let mutable keyPair = {KeyPair.privateKey ="";KeyPair.publicKey=""}
    let mutable users = Dictionary<string,userInfomation>()
    let mutable tweetsTags = Dictionary<string,List<string>>()
    let mutable mentionTweet = Dictionary<string,List<string>>()
    let mutable tweetsMentions = Dictionary<string,List<string>>()
    let mutable sockets = Dictionary<string,WebSocket>()
    let mutable digitalSigns = Dictionary<string,string>()
    let checkExist (userName:string):bool =
        if (users.ContainsKey(userName)) then
            true
        else
            false         
    let checkStatus (userName:string): bool =
        if (users.ContainsKey(userName)) then
            if (users.[userName].live) then
                true
            else
                false
        else
            false
            
    let registration (userToAdd: Register) :MessageInSocket =
        let message = {MessageInSocket.status = -1 ; MessageInSocket.Content = null ; MessageInSocket.alerts = "registration success" }
        if checkExist userToAdd.Name then
            printfn "user %s exist......" userToAdd.Name
            message.alerts <- "user already exist"
            message.status <- -1
            message
        else
            printfn "registration %s success" userToAdd.Name
            message.status <- 1
            let user = {userInfomation.id = userToAdd.Name
                        userInfomation.live = true
                        userInfomation.password = userToAdd.Password
                        userInfomation.tweets = List<string>()
                        userInfomation.subscript = List<string>();
                        userInfomation.follower = List<string>() }
            users.Add(userToAdd.Name, user)
            message
            
    let logout (userToLogout:LogoutUser) :MessageInSocket =
        let message = {MessageInSocket.status = -1 ; MessageInSocket.Content = null ; MessageInSocket.alerts = "logout success" }
        let hmacLocal = CryptoUtils.HMACSign(userToLogout.Name,digitalSigns.[userToLogout.Name])
        printfn "digital sign: %s and localSign: %s" userToLogout.HMACname hmacLocal
        if hmacLocal <> userToLogout.HMACname then  
            message.status <- -3
            message.alerts <- "digital signs changed, data error"
            message
        else 
            if (checkExist userToLogout.Name) then
                if checkStatus userToLogout.Name = true then
                    message.status <- 1
                    printfn "user %s logout......" userToLogout.Name
                    users.[userToLogout.Name].live <- false
                    if (sockets.Remove(userToLogout.Name) = true) then
                         printfn "disconnected"
                    else
                         printfn "error disconnected"
                    message
                else
                    printfn "user %s is not alive......" userToLogout.Name
                    message.status <- 0
                    message.alerts <- "user not login"
                    message
            else
                printfn "user %s not exist......" userToLogout.Name
                message.status <- -1
                message.alerts <- "user not exist"
                message
    
    let getPublickey (infor : PublicKeyRequest):MessageInSocket =
        let message = {MessageInSocket.status = -1 ; MessageInSocket.Content = null ; MessageInSocket.alerts = "get publickey success" } 
//        rsa <- new RSACryptoServiceProvider(2048)
        let keypairs = CryptoUtils.generateKey()
        keyPair.privateKey <- keypairs.privateKey
        keyPair.publicKey <- keypairs.publicKey
        let secrectKeyForSign = infor.Name + (DateTime.Now.Millisecond.ToString())
        if(digitalSigns.ContainsKey(infor.Name) = false) then
            digitalSigns.Add(infor.Name,secrectKeyForSign)
        else
            digitalSigns.[infor.Name] <- secrectKeyForSign
//        secrectKeyForSign <- infor.message
        printfn "secrectKeyForSign: %s" secrectKeyForSign
        let publickeyAndSecrect = [|keyPair.publicKey;secrectKeyForSign|]
//        printfn "public: %s" publickey.[0]
//        printfn "private: %s" privateKey
//        privatekey <- CryptoUtils.generatePrivatekey()
        message.status <- 1
        message.alerts <- infor.Name+" success getkeys"
        message.Content <- publickeyAndSecrect
        message
//        let login (userToLogin:LoginUser) :MessageInSocket =
//            let message = {MessageInSocket.status = -1 ; MessageInSocket.Content = null ; MessageInSocket.alerts = "login success" }
//            if checkExist userToLogin.Name then
//                if checkStatus userToLogin.Name = true then
//                    message.status <- 0
//                    users.[userToLogin.Name].live <- true
//                    message.alerts <- "user already login"
//                    printfn "user %s already login......" userToLogin.Name
//                    message
//                else
//                    printfn "user has encrypt pass %s :" userToLogin.Password
//                    let password = CryptoUtils.RsaDecrypt(privatekey,userToLogin.Password)
//                    if (users.[userToLogin.Name].password.Equals userToLogin.Password) then
//                        printfn "user %s login success......" userToLogin.Name
//                        users.[userToLogin.Name].live <- true
//                        message.status <- 1
//                        message
//                    else
//                        printfn "user %s login wrong password......" userToLogin.Name
//                        message.status <- -1
//                        message.alerts <- "user wrong password"
//                        message
//            else
//                printfn "user %s not exist failed login......" userToLogin.Name
//                message.status <- -2
//                message.alerts <- "user not exist"
//                message   
    let login (userToLogin:LoginUser) :MessageInSocket =
        let message = {MessageInSocket.status = -1 ; MessageInSocket.Content = null ; MessageInSocket.alerts = "login success" }
        let hmacLocal = CryptoUtils.HMACSign(userToLogin.Name,digitalSigns.[userToLogin.Name])
        
        printfn "digital sign: %s and name: %s" userToLogin.HMACname userToLogin.Name
        printfn "digital sign: %s and localSign: %s" userToLogin.HMACname hmacLocal
        if hmacLocal <> userToLogin.HMACname then  
            message.status <- -3
            message.alerts <- "digital signs changed, data error"
            message
        else
            if checkExist userToLogin.Name then
                    if checkStatus userToLogin.Name = true then
                        message.alerts <- "user already login"
                        printfn "user %s already login......" userToLogin.Name
    //                printfn "user has encrypt pass %s :" userToLogin.Password
                    let passwordwithTimeStamp = CryptoUtils.RsaDecrypt(keyPair.privateKey,userToLogin.Password)
                    printfn "user has decrypt passwordwithTimeStamp: %s" passwordwithTimeStamp
                    let clientTimeStampString = passwordwithTimeStamp.Substring((passwordwithTimeStamp.Length-13),13)
                    let clientTimeStamp = Convert.ToUInt64(clientTimeStampString)
                    let password = passwordwithTimeStamp.Substring(0,(passwordwithTimeStamp.Length-13))
                    printfn "user has decrypt pass: %s" password
                    printfn "user has original pass: %s " userToLogin.Password
                     
                    let serverStartTime = System.TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1));
                    let timeStamp = (uint64 (System.DateTime.Now - serverStartTime).TotalMilliseconds)
                    let eclipse = timeStamp - clientTimeStamp
                    printfn "time eclipse: %d" eclipse
                    if (users.[userToLogin.Name].password = password && eclipse < (uint64 60000)) then
                        printfn "user %s login success......" userToLogin.Name
                        users.[userToLogin.Name].live <- true
                        message.status <- 1
                        message
                    else
                        printfn "user %s login wrong password or log time exceeded 1 second......" userToLogin.Name
                        message.status <- -1
                        message.alerts <- "user wrong password"
                        message
            else
                printfn "user %s not exist failed login......" userToLogin.Name
                message.status <- -2
                message.alerts <- "user not exist"
                message
    let getAllTweets (allTweets:AllTweets) :MessageInSocket =
        printfn "getting all tweets ===" 
        let message = {MessageInSocket.status = -1 ; MessageInSocket.Content = null ; MessageInSocket.alerts = "get all tweets success" }
        let hmacLocal = CryptoUtils.HMACSign(allTweets.Name,digitalSigns.[allTweets.Name])
        printfn "digital sign: %s and localSign: %s" allTweets.HMACname hmacLocal
        if hmacLocal <> allTweets.HMACname then  
            message.status <- -3
            message.alerts <- "digital signs changed, data error"
            message
        else
            if checkExist allTweets.Name then
                if checkStatus allTweets.Name = true then
                    let tweets = users.[allTweets.Name].tweets.ToArray()
                    message.Content <- tweets
    //                printfn "getting tweets[0]= %s"  (message.Content.[0])
                    message.status <- 1
                    message.alerts <- "get tweets success"
    //                printfn "user %s get all tweets......" allTweets.Name
                    message
                else
                    
                    printfn "user %s not login......" allTweets.Name
                    message.status <- -1
                    message.alerts <- "user not login"
                    message
            else
                printfn "user %s not exist ......" allTweets.Name
                message.status <- -2
                message.alerts <- "user not exist"
                message
    let getAlltagsTweets (tagTweet:TagTweets) :MessageInSocket=
        printfn "tag====================="
        let message = {MessageInSocket.status = -1 ; MessageInSocket.Content = null ; MessageInSocket.alerts = "get all tweets of tag success" }
        let hmacLocal = CryptoUtils.HMACSign(tagTweet.Name,digitalSigns.[tagTweet.Name])
        printfn "digital sign: %s and localSign: %s" tagTweet.HMACname hmacLocal
        if hmacLocal <> tagTweet.HMACname then  
            message.status <- -3
            message.alerts <- "digital signs changed, data error"
            message
        else
            if checkExist tagTweet.Name then
                if checkStatus tagTweet.Name = true then
                    if(tweetsTags.ContainsKey(tagTweet.Tag)) then
    //                    for i in 0..tweetsTags.[tagTweet.Tag].Count-1 do
    //                        printf "tag %s has: %s" tagTweet.Tag tweetsTags.[tagTweet.Tag].[i]
    //                        message.Content.Add tweetsTags.[tagTweet.Tag].[i]
                        message.Content <- tweetsTags.[tagTweet.Tag].ToArray()
                        message.status <- 1
                        message.alerts <- "get all tweets of tag success"
                        message
                    else
                        message.status <- 0
                        message.alerts <- "no tweet tags found"
                        message
                else
                    printfn "user %s not login......" tagTweet.Name
                    message.status <- -1
                    message.alerts <- "user not login"
                    message
            else
                printfn "user %s not exist ......" tagTweet.Name
                message.status <- -2
                message.alerts <- "user not exist"
                message  
    let getAllMentionTweets (mention:MentionTweets) :MessageInSocket =
//        printfn "%s prepare get mentioned tweets====" mention.MentionName
        let message = {MessageInSocket.status = -1 ; MessageInSocket.Content = null ; MessageInSocket.alerts = "get all mention tweets success" }
        let hmacLocal = CryptoUtils.HMACSign(mention.MentionName,digitalSigns.[mention.MentionName])
        printfn "digital sign: %s and localSign: %s" mention.HMACname hmacLocal
        if hmacLocal <> mention.HMACname then  
            message.status <- -3
            message.alerts <- "digital signs changed, data error"
            message
        else
            if checkExist mention.MentionName then
                if checkStatus mention.MentionName = true then
                    if(mentionTweet.ContainsKey(mention.MentionName)) then
                        printfn "%s get mentioned tweets" mention.MentionName
                        message.Content <- mentionTweet.[mention.MentionName].ToArray()
                        for i in 0..message.Content.Length-1 do
                            printfn "mentioned tweets have %s" message.Content.[i] 
                        message.status <- 1
                        message.alerts <- "get all mentions tweets success"
                        message
                    else
                        message.alerts <- "no tweet mentioned found"
                        message.status <- 0
                        message
                else
                    printfn "user %s not login......" mention.MentionName
                    message.status <- -1
                    message.alerts <- "user not login"
                    message
            else
                printfn "user %s not exist ......" mention.MentionName
                message.status <- -2
                message.alerts <- "user not exist"
                message
                
    let system = ActorSystem.Create("TwitterServer")
    type ServerActor() =
        inherit Actor()
        override x.OnReceive message =
            let msg = message :?> MessageInActor
            match msg with
            |TweetContents (websocket,contents) ->
                printfn "tweets %s" contents
                let response = "Tweet success with: "+contents
                let result = response
                             |> Encoding.ASCII.GetBytes
                             |> ByteSegment
                let s = socket{
                            do! websocket.send Text result true
                       }
                Async.StartAsTask s |> ignore
            |TweetBySubscriber (websocket, name,contents) ->
                let response = "Tweet "+contents+" is from "+name
                let result = response
                             |> Encoding.ASCII.GetBytes
                             |> ByteSegment
                let s = socket{
                            do! websocket.send Text result true
                       }
                Async.StartAsTask s |> ignore
            |TweetToMention (websocket, name, contents) ->
                let response = "Tweet "+contents+" mentioned from "+name
                let result = response
                             |> Encoding.ASCII.GetBytes
                             |> ByteSegment
                let s = socket{
                            do! websocket.send Text result true
                       }
                Async.StartAsTask s |> ignore
            |AddSubscribe (websocket, name) ->
                let response = name + " want to subscribe"
                let result = response
                             |> Encoding.ASCII.GetBytes
                             |> ByteSegment
                let s = socket{
                            do! websocket.send Text result true
                       }
                Async.StartAsTask s |> ignore        
                
    let Serveractor = system.ActorOf(Props.Create(typedefof<ServerActor>),name="server")    
    
    let addFolloOrSubcription (subscription: SubscribeUser) :MessageInSocket=
            let message = {MessageInSocket.status = -1 ; MessageInSocket.Content = null ; MessageInSocket.alerts = "get all mention tweets success" }           
            let hmacLocal = CryptoUtils.HMACSign(subscription.Follower,digitalSigns.[subscription.Follower])
            printfn "digital sign: %s and localSign: %s" subscription.HMACname hmacLocal
            if hmacLocal <> subscription.HMACname then  
                message.status <- -3
                message.alerts <- "digital signs changed, data error"
                message
            else
                if checkExist subscription.Follower then
                    if checkStatus subscription.Follower = true then
                        if(users.ContainsKey(subscription.Subscriber)=false) then
                            message.alerts <- "user to subscribe not found"
                            message.status <- 0
                            message   
                        else
                            printfn "form %s to %s " subscription.Follower subscription.Subscriber
                            if(users.[subscription.Subscriber].follower.Contains(subscription.Follower) ||  users.[subscription.Follower].subscript.Contains(subscription.Subscriber)) then 
                                printfn "already subscribed"
                                message.status <- 0
                                message.alerts <- "already subscribed"
                                message
                            else
                                users.[subscription.Subscriber].follower.Add(subscription.Follower)
                                users.[subscription.Follower].subscript.Add(subscription.Subscriber)
                                Serveractor <! AddSubscribe(sockets.[subscription.Subscriber],subscription.Follower)
                                message.status <- 1
                                message.alerts <- "subscribed successful"
                                message
                    else
                        printfn "user %s not login......" subscription.Follower
                        message.status <- -1
                        message.alerts <- "user not login"
                        message
                else
                    printfn "user %s not exist ......" subscription.Follower
                    message.status <- -2
                    message.alerts <- "user not exist"
                    message
    let tweets (tweet:Tweet) :MessageInSocket=
        let message = {MessageInSocket.status = -1 ; MessageInSocket.Content = null ; MessageInSocket.alerts = "Tweets success"}
        let hmacLocal = CryptoUtils.HMACSign(tweet.Name,digitalSigns.[tweet.Name])
        printfn "digital sign: %s and localSign: %s" tweet.HMACname hmacLocal
        if hmacLocal <> tweet.HMACname then  
            message.status <- -3
            message.alerts <- "digital signs changed, data error"
            message
        else
            if checkExist(tweet.Name) = false then              
                printfn "user: %s not exist" tweet.Name
                message
            else
                if (checkStatus(tweet.Name) =false ||  sockets.ContainsKey(tweet.Name)) = false then
                    printfn "user %s is not login or no socket connected" tweet.Name
                    message.status <- 0
                    message
                else     
                    users.[tweet.Name].tweets.Add(tweet.TweetContent)
                    
                    Serveractor <! TweetContents(sockets.[tweet.Name],tweet.TweetContent)
                    let mentions = RegexUtils.findMention tweet.TweetContent
                    let tags = RegexUtils.findTags tweet.TweetContent
        //                for i in 0..mentions.Count-1 do
        //                    printfn "tweet has mentioned %s" mentions.[i]
                    for i in 0..tags.Count-1 do
        //                printfn "tweet has tag %s" tags.[i]
                        if(tweetsTags.ContainsKey(tags.[i]) = false) then
                            tweetsTags.Add(tags.[i],List<string>())
                        tweetsTags.[tags.[i]].Add(tweet.TweetContent)
    //                if(tweetsTags.ContainsKey(contends) = false) then
    //                    tweetsTags.Add(contends,List<string>())
    //                    for i in 0..tags.Count-1 do
    //                        if (tweetsTags.[contends].Contains(tags.[i])=false) then
    //                            tweetsTags.[contends].Add(tags.[i])      
                    for i in 0..users.[tweet.Name].follower.Count-1 do
                        let currentUser = users.[tweet.Name].follower.[i]
                        users.[currentUser].tweets.Add(tweet.TweetContent)
                        if(sockets.ContainsKey(currentUser)) then
                            printfn "%s tweet to subscriber %s" tweet.Name currentUser
                            Serveractor <! TweetBySubscriber(sockets.[currentUser],tweet.Name,tweet.TweetContent)
                        
    //              if(users.ContainsKey(id) = true) then
                    for i in 0..mentions.Count-1 do              
                        if users.ContainsKey(mentions.[i]) = true then
                            if(mentionTweet.ContainsKey(mentions.[i]) = false) then
                                mentionTweet.Add(mentions.[i],List<string>())
                            if(mentionTweet.[mentions.[i]].Contains(tweet.TweetContent) = false) then
                                mentionTweet.[mentions.[i]].Add(tweet.TweetContent)
                    if(tweetsMentions.ContainsKey(tweet.TweetContent) = false) then
                        tweetsMentions.Add(tweet.TweetContent, List<string>())
        //                printfn "if xxxxxxxxxxxxxxxxxxxxxxxxx into %d" (mentions.Count)              
                    for i in 0..mentions.Count-1 do
        //                printfn "now add mention %s" mentions.[i]
                        if users.ContainsKey(mentions.[i]) = true then
                            if(tweetsMentions.[tweet.TweetContent].Contains(mentions.[i]) = false) then
                                tweetsMentions.[tweet.TweetContent].Add(mentions.[i])
                            printfn "%s to %s" tweet.Name mentions.[i]                                     
                            if users.[mentions.[i]].live = true then
        //                              printfn "mentioned user is alive can tweet %s" contends
                                  users.[mentions.[i]].tweets.Add(tweet.TweetContent)
                                  if(sockets.ContainsKey(mentions.[i])) then
                                        Serveractor <! TweetToMention(sockets.[mentions.[i]],tweet.Name,tweet.TweetContent)
                            else
                                printfn "mentioned user is dead"
                    message.status <- 1
                    message
     
    let retweetContents(retweet:Retweet):MessageInSocket =
//         let mutable message = {MessageInSocket.status = -1 ; MessageInSocket.Content = List<string>() ; MessageInSocket.alerts = "Retweets success"}
         let tweetStruct = {Tweet.Name = retweet.Name; Tweet.TweetContent = retweet.TweetContent;Tweet.HMACname=retweet.HMACname} |> tweets
         tweetStruct
    
    let websocketHandler (webSocket : WebSocket) (context: HttpContext) =
        socket {
            let mutable loop = true
            while loop do
                  let! msg = webSocket.read()
                  match msg with
                  | (Text, data, true) ->
                    printfn "%A" Text
                    let str = UTF8.toString data
                    printfn "message %s " str
                    let msg = JsonConvert.DeserializeObject<ReceiveMessage>(str)
                    printfn "message %s with: %s" msg.types msg.content
                    match msg.types with
                    | "login" ->
                    // the message can be converted to a string
                    // printfn "%A" Text
                        let username = msg.content 
                        // printfn "%s" str
                        if sockets.ContainsKey(username) then
                            printfn "%s already login or old connection has been closed accidentally" username
                        else
                            sockets.Add(username,webSocket)
                            printfn "connected to %s websocket" username
                    | _ -> 
                        let response = sprintf "response to %s" msg.content
                        let byteResponse = response
                                           |> Encoding.ASCII.GetBytes
                                           |> ByteSegment              
                        do! webSocket.send Text byteResponse true

                  | (Close, _, _) ->
//                    printfn "socket closed"
                    let emptyResponse = [||] |> ByteSegment
                    do! webSocket.send Close emptyResponse true
                    loop <- false
                  | _ ->
                      printfn "receive other message"
            }
    let JsonDeserialize<'a> json =
        JsonConvert.DeserializeObject(json, typeof<'a>) :?> 'a    
    let setCORSHeaders =
        setHeader  "Access-Control-Allow-Origin" "*"
        >=> setHeader "Access-Control-Allow-Headers" "content-type"    
    let Ajaxlogin =
        request (fun requstBody ->
        requstBody.rawForm
        |> Encoding.UTF8.GetString
        |> JsonDeserialize<LoginUser>
        |> login
        |> JsonConvert.SerializeObject
        |> OK
        )
        >=> setMimeType "application/json"
        >=> setCORSHeaders        
    let Ajaxlogout =
        request (fun requstBody ->
        requstBody.rawForm
        |> Encoding.UTF8.GetString
        |> JsonDeserialize<LogoutUser>
        |> logout
        |> JsonConvert.SerializeObject
        |> OK
        )
        >=> setMimeType "application/json"
        >=> setCORSHeaders 
    let Ajaxregister =
        request (fun requstBody ->
        requstBody.rawForm
        |> Encoding.UTF8.GetString
        |> JsonDeserialize<Register>
        |> registration
        |> JsonConvert.SerializeObject
        |> OK
        )
        >=> setMimeType "application/json"
        >=> setCORSHeaders
    let AjaxTweet = 
        request (fun requstBody ->
        requstBody.rawForm
        |> Encoding.UTF8.GetString
        |> JsonDeserialize<Tweet>
        |> tweets
        |> JsonConvert.SerializeObject
        |> OK
        )
        >=> setMimeType "application/json"
        >=> setCORSHeaders
    let AjaxGetTweets (allTweetGet:AllTweets) =
        allTweetGet
        |> getAllTweets
        |> JsonConvert.SerializeObject
        |> OK
        >=> setMimeType "application/json"
        >=> setCORSHeaders
        
    let AjaxgetTagTweets (getTags:TagTweets)=
        getTags
        |> getAlltagsTweets
        |> JsonConvert.SerializeObject
        |> OK
        >=> setMimeType "application/json"
        >=> setCORSHeaders
    
    let AjaxMentionTweets (mentionTweets:MentionTweets)=
        mentionTweets
        |> getAllMentionTweets
        |> JsonConvert.SerializeObject
        |> OK
        >=> setMimeType "application/json"
        >=> setCORSHeaders
    
    let AjaxGetPublickey (message:PublicKeyRequest) =
        message
        |> getPublickey
        |> JsonConvert.SerializeObject
        |> OK
        >=> setMimeType "application/json"
        >=> setCORSHeaders
        
    let AjaxRetweet =
        request (fun requstBody ->
        requstBody.rawForm
        |> Encoding.UTF8.GetString
        |> JsonDeserialize<Retweet>
        |> retweetContents
        |> JsonConvert.SerializeObject
        |> OK
        )
        >=> setMimeType "application/json"
        >=> setCORSHeaders
    

    let AjaxSubscribe =
        request (fun r ->
        r.rawForm
        |> Encoding.UTF8.GetString
        |> JsonDeserialize<SubscribeUser>
        |> addFolloOrSubcription
        |> JsonConvert.SerializeObject
        |> OK
        )
        >=> setMimeType "application/json"
        >=> setCORSHeaders

    let allow_cors : WebPart =
        choose [
            OPTIONS >=>
                fun context ->
                    context |> (
                        setCORSHeaders
                        >=> OK "CORS approved" )
        ]
    let app =
        choose
            [ 
                path "/websocket" >=> handShake websocketHandler 
                allow_cors
                GET >=> choose
                    [ 
                        path "/" >=> OK "Hello World"
                        pathScan @"/gettweets/%s/%s" (fun (username,hmacNname) ->
                            let typeGetTweets = {AllTweets.Name = username;AllTweets.HMACname = hmacNname}
                            AjaxGetTweets typeGetTweets
                            )
                        pathScan @"/getmentions/%s/%s" (fun (mentionName,hmacNname) ->
                                printfn "%s wants mentions" mentionName
                                let typeMentionTweets = {MentionTweets.MentionName = mentionName;MentionTweets.HMACname=hmacNname}
                                AjaxMentionTweets typeMentionTweets
                            )
                        pathScan @"/gethashtags/%s/%s/%s" (fun (name,hashtag,hmacNname) ->
//                                printfn "%s wants tags %s" name hashtag
                                let typeTagTweets = {TagTweets.Tag = hashtag ; TagTweets.Name = name; TagTweets.HMACname=hmacNname }
                                AjaxgetTagTweets typeTagTweets
                            )
                        pathScan @"/getPublic/%s" (fun (name) ->
                                let messages = {PublicKeyRequest.Name = name}
                                AjaxGetPublickey messages
                            )
                    ]

                POST >=> choose
                    [   
                        path "/newtweet" >=> AjaxTweet 
                        path "/register" >=> Ajaxregister
                        path "/login" >=> Ajaxlogin
                        path "/logout" >=> Ajaxlogout
                        path "/follow" >=> AjaxSubscribe
                        path "/reTweet" >=> AjaxRetweet
                    ]

                PUT >=> choose
                    [ ]

                DELETE >=> choose
                    [ ]
            ]

    [<EntryPoint>]
    let main argv =
        startWebServer { defaultConfig with bindings = [HttpBinding.createSimple HTTP "127.0.0.1" 7777];
                                          logger = Targets.create Verbose [||] } app
        0