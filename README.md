# Twitter Simulator
Twitter Simulator is a  Fsharp based project which contains 2 parts, one for Tweet Engine server with Websocket and the other for client using html web-site (JavaScript). 

This project also implemented cryptographic authentication.



#### Table of Contents
1. [Description](#Description)
2. [How to Use](#How to Use)
3. [Work be Done](#Work)
4. [Results and Performance](#Test)
5. [Other Info](#Other Info)

<a name="Description"></a>·

I. Description
----
### Problem definition

- Use Suave library to implement a WebSocket interface.

- Design a JSON based API that  represents all messages and replies (including errors)
- Implement a public key-based authentication method for your interface

- A user, upon registration, provides a public key (RSA-2048)
- When the user login via WebSockets, it first authenticates using a challenge-based algorithm

  - The server sends a 256-bit challenge (public key)
  - The client forms a message containing the challenge, the current time in milliseconds and digitally signs it.
  - The server sends a confirmation or an error
  - The server is not allowed to reuse the challenge. 
- The user HMAC signs every message sent and The HMAC is computed over the serialized JSON content.



### Technic

- Ajax request
- WebSocket
- Akka Actor
- Fsharp, JavaScript
- Public-key Authentication, Digital Sign
- Rest Api

<a name="How to Use"></a>

II. How to Use
----
#### Use following commands  and run the program

Note:

1. Server end contains three fs files.

    - Program.fs is the main entry of Twitter engine.

    - MessageType.fs contains messages used to communicate between server and client and they can be transformed to Json type. Also, it contains the message of Akka actor.

   - Utils.fs contains the functions that use regex method to find tags and mentions in tweets and  it also contains the functions of cryptograph which are RSA-2048 public-key authentication and HMAC digital sign.

2. Client is a html web-site with JavaScript front-end program and CSS style.

   - index.html is the web-site with two pages, one for login/register and the other for the operations of twitter simulation.
   - JavaScript files in js directory are essential because some cryptographic codes are implemented in them.
   - CSS files in css directory is also useful or the page will be ruined.

Commands:

```
Command for server: dotnet run TwitterWeb
for client: just open index.html and have fun!
```

Note: you need to change the ip address if suave can't bind the port. The code is in the main function in Program.fs and you just need to change the ip and port. If you changed the ip address in server, you need also change the ip in client. These ip addresses are in JavaScript functions in index.html and you can change them by replacing.

#### Nice try

I have implemented the server along with the client web-site in Tencent cloud remote server on CentOS 7. The basic functions work well but the connection is not stable. I assume it happens because of the limitation of server resources and it comes from internet bandwidth or CPU itself(only has one core and 1G RAM).

So if you want to try it, you can open

http://www.yybcloud.cn/

But the server(Twitter engine) needs to be run with the opening of linux shell. So you can contact me to open it if you want to try. (This probably can be solved by adding a supervising service and I can try it later.)  

<a name="Work"></a>

III. Work be Done
----

#### Server

A server of twitter engine is created to serve for data preservation and operation of tweets. The connection between server and client has two kinds, WebSocket and Ajax request. Something needs to be mentioned is that the functions of subscription and tweet content is implemented by Akka actor and this is where websocket requests are handled. Due to the large volume of connections from web sites, akka actor may be a better way to deal with these requests because it will always running in background and handle different kinds of requests.

- WebSocket: 

  Used to build a long connection between server and client. When a user login, it builds this connection until they are disconnected due to logout, network failure or crush on server.

  To be specific, when a user is logged in, a web socket is build between server and user. Then this socket is added into a dictionary. This design is build for a propose which is, when user A mentions user B, B can receive this tweet immediately, and this also happens when A subscribes B and B tweets a tweet, A also need to receive the tweet at once.

  Sample code for handle websocket  is:

  ```F#
  let websocketHandler (webSocket : WebSocket) (context: HttpContext) =
      socket {
          let mutable loop = true
          while loop do
                let! msg = webSocket.read()
                match msg with
                | (Text, data, true) ->
                | (Close, _, _) ->
                	loop<-false
                | _ ->()
       }
  ```

- Ajax request: 

  This is used to make a short request between server and client.  I have used two kinds of request in the program, one is "POST" and the other is "GET". 

  - "POST" method can deliver a serialized Json type message to server and then also get a Json type response.  
  - "GET" method is to use restful API to get some information from server with Json message. For example, when I want  to get all my tweets from server, I just need to use "ajax GET http://xxx:xxx/gettweets/myname" and the server will know I need to get my tweets and my name is "myname". 
  
  Sample code for handle ajax request is:
  
  ```F#
  let Ajaxhandle =
          request (fun r ->
          r.rawForm
          |> Encoding.UTF8.GetString
          |> JsonDeserialize<SubscribeUser>
          |> handle_function
          |> JsonConvert.SerializeObject
          |> OK
          )
          >=> setMimeType "application/json"
          >=> setCORSHeaders
  ```
  
  

#### Client

In client side, the web page contains two parts, login/registration and simulation. (These parts are implemented in JavaScript)

- Login/Registration:

  When you want to access to the twitter simulator, registration and login are necessary. When a user registers to the twitter, the front-end js function will check if the password and re-enter password is the same and then pass it to the server.  Then the user can log in to the server with the password. The server will check if the password matches the data that is already stored and then give a response. If the response is "OK", the user will be able to enter the simulation page. 

- Simulation:

  There are 5 parts in simulation, tweet something, subscribe somebody, get tweets, get tweets with hashtags and get tweets that mentioned someone itself. Also a simple log message box is showed in the bottom to let you know if you are subscribed or mentioned. And you can also get live tweets by some one you are following.

  These functions are all triggered by clicking the bottoms. All the messages are passed by Json type through websocket or ajax requests and can get feed backs from the server. According to the feedback, the website can show results or give alerts.

  Function for websocket:
  
    ```javascript
    function startWebSocket(){
    		var wsUri = "ws://xxx:xxxx/websocket";
    		websocket = new WebSocket(wsUri);
    		websocket.onopen = function(evt) { onOpen(evt) };
    		websocket.onclose = function(evt) { onClose(evt) };
    		websocket.onmessage = function(evt) { onMessage(evt) };
    		websocket.onerror = function(evt) { onError(evt) };
    	}
    function onOpen(evt){}//socket built
    function onClose(evt){}//socket close
    function onMessage(evt){}//receive from server
    function onError(evt){}//some error happens
    function doSend(message){websocket.send(message)}
    //someting to send to server
    ```
  
    The sample code of ajax is showed below:
  
    ```javascript
    url = "http://xxx:xxx/function"
    data = "push something to server"
    $.ajax({
    			type: "POST", //or "GET"
    			url: url,
    			data: data,
    			dataType: "json", 
    			success: function(data){
    				alert(data.alerts);  //alert something
    //The situation of requests which are handled can be showed by status 
    				if(data.status < 0){ 
    				}else{
    				}			
    			}
    		});
    ```

#### Bonus of Cryptograph

##### Challenge/Response with RSA-2048:

Challenge/Response is a security method used to encrypt the message sent by client to the server. When the client build a connection with the server, the server will generate a pair of keys that contains public key and private key. Public key will give to the client as a challenge. If the user want to re-connect(login) to the server, it needs to encrypt the password with the public key. But because no salt is added to the password, some hackers can use the encrypted key to login. That's why the password needs to add a time stamp (UNIX time) (Note that UNIX time stamp is based on second, I try to use millisecond stamp to get more accurate time). Then the server can decrypt the key with the private key and get the password and time stamp. If the time stamp shows that the message is delivered for over 1 second, the password can be expired and the user needs to try again.

- Key generation:

  ```F#
  let generateKey() :KeyPair =
          let rsa = new RSACryptoServiceProvider(2048)
          let privatekey = rsa.ToXmlString(true)
          let publickey = rsa.ToXmlString(false)
          let keys = {KeyPair.privateKey = privatekey ; 				KeyPair.publicKey = publickey}
          keys

- Client encrypt:

  ```javascript
  publicKey = data.Content[0] //get publickey from server
  var rsa = new RSACryptoServiceProvider(2048);
  rsa.FromXmlString(publicKey);
  var rsaParamsPublic = rsa.ExportParameters(false);
  var timestamp = new Date().getTime() //get time stamp 
  var Time = pwd + timestamp.toString() //combine to pass
  var decryptedBytes = Encoding.UTF8.GetBytes(Time);
  rsa.ImportParameters(rsaParamsPublic);
  //encrypt key
  var encryptedBytes = rsa.Encrypt(decryptedBytes,true);
  var encryptedString = ToBase64String(encryptedBytes);

- Server decrypt:

  ```F#
  let Decrypt (privatekey:string,content:string):string =
      let rsa = new RSACryptoServiceProvider(2048)
      rsa.FromXmlString(privatekey)
      let byteContent = rsa.Decrypt(Convert.FromBase64String(content),true)
          let result = byteContent |> 					 						Encoding.UTF8.GetString
          result
  
  //Decrypt key:
  let passTime = Decrypt(keyPair.privateKey,Password)
  //time stamp is a string with 13 digits
  let TimeStampString = 
  passTime.Substring((passwordwithTimeStamp.Length-13),13)
  //convert to number
  let clientTimeStamp = Convert.ToUInt64(clientTimeStampString)
  //password should be the string without 13 digits
  let password = passwordwithTimeStamp.Substring(0,(passwordwithTimeStamp.Length-13))
  
  //if password match and time stamp is less than 1s, the //login is successful.
  //60000 is from 60*1*1000. Note that I didn‘t use UNIX //time stamp but use millisecond time stamp to get a //more accurate time
   if (users.password = password && eclipse < (uint64 60000)) then
   loginSuccess
   else
   failed
  ```



##### Digital Sign with message

Digital sign is like a secret key shared by client and server. When a user is created, the server gives a secret key to the client and use HMAC method to sign the message. The signed message is hashed and can't be decrypted so the client should pass the original message along with the encrypted(hashed) message to the server. Then the server will use the same secret key and the same HMAC method to encrypt the message again, if the encrypted key by server is the same as the encrypted key passed by client, then it means that the original message is not changed illegally during the network transportation. 

The hash method I used in this project is HMAC-SHA256 and the secret key is randomly generated by the user's name with the time stamp when they log in.

- Key generation:

  ```F#
  //Use name and time to random generate key
  let secrectKey = Name + (DateTime.Now.Millisecond.ToString())
  ```

- Encrypt with key in client:

  ```javascript
  var hash = CryptoJS.HmacSHA256(username, secretKeyForSign);
  var hmacName = hash.toString(CryptoJS.enc.Base64);
  ```

- Encrypt with key in server:

  ```F#
  let HMACSign (message:string,secretKey:string) :string =
          let key = secretKey |>Encoding.UTF8.GetBytes
          let mess = message |>Encoding.UTF8.GetBytes
          let hmacsha256 = new HMACSHA256(key)
          let hashmessage = hmacsha256.ComputeHash(mess)
  //Due to the type different from language to language, I //tried so many times to find the right way to decode /the encrypted message. In F#, it should be converted to hexstring and change it to lower case letter.
          let result = Convert.ToHexString(hashmessage).ToLowerInvariant()
          result
  ```

- Check the message with digital sign

  ```F#
  let hmacLocal = HMACSign(Name,secretKey)
  if hmacLocal <> cient.HMACname then  
  	success
  else
  	failed
  ```



IV. Performance
----

#### Performance

##### The performance before and after bonus implement

After adding bonus part in the project, the time used for login and data exchange between server and client should be slower. But this difference is not significant that we even can't feel the different.

It's probably because the computation time period of hashing and SHA-256 is not so long for small data. In this project, the data that are passed are just names or simple time of response. 

##### The largest network managed to deal with:

- The number of users I tested in local program is 5 and it can be more, and none of these users make mistakes.

- The max number of users I tested on remote server using internet is 4 and the web socket connection can be disconnected after a short time. The reason that cause this failure is most likely the limitation of server resource. And when more users log in, the connection between user and server become more unstable.




## V. Future Work

#### In the future, some work can be done to perfect this project:

- The database implementation:

  The data in this project are stored in Dictionary or Lists. But this can not support large data and has some security problems. But if we can use Oracle or MySQL database to store them, problem can be solved. Besides, these data are cached in memory  and can be erased when the server restart. But the database can also permanently store them.  

- The performance test in large data can be made. For example, we can encrypt tweets with a very long content and then take the performance test. 

<a name="Other Info"></a>

VI. Other Info
----
Author: Yangbo Yu

Finish Time: 12/14/2021
