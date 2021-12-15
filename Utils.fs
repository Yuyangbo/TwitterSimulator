namespace TwitterWeb
open System
open System.Security.Cryptography
open System.Text
open System.Text.RegularExpressions
open System.Collections.Generic
module RegexUtils =
    let findTags (contents:string) :List<string> =
        let mutable tags = List<string>()
        let mutable tag = ""
        let pattern = @"#[\w]+"
        let mutable m = Regex(pattern)
        let matchList = m.Matches contents
        for i in 0..matchList.Count-1 do
            tag <- matchList.Item i |>string
            tags.Add(tag.Substring(1,tag.Length-1))
        tags
        
    let findMention (contents:string) :List<string> =
        let mutable mentions = List<string>()
        let mutable mention = ""
        let pattern = @"@[\w]+"
        let mutable m = Regex(pattern)
        let matchList = m.Matches contents
        for i in 0..matchList.Count-1 do
            mention <- matchList.Item i |>string
    //                printfn "%s" (mention.Substring(1,mention.Length-1))
            mentions.Add (mention.Substring(1,mention.Length-1))
        mentions
        
module CryptoUtils = 
    let generateKey() :KeyPair =
        let rsa = new RSACryptoServiceProvider(2048)
        let privatekey = rsa.ToXmlString(true)
        let publickey = rsa.ToXmlString(false)
        let keys = {KeyPair.privateKey = privatekey ; KeyPair.publicKey = publickey}
        keys
        
    let RsaEncrypt (publickey:string, content:string) :string =
        let rsa = new RSACryptoServiceProvider()
        rsa.FromXmlString(publickey)
        let utfContent = content |> Encoding.UTF8.GetBytes
        let byteContent = rsa.Encrypt(utfContent,true)
        let result = Convert.ToBase64String(byteContent)
        result    
    let RsaDecrypt (privatekey:string,content:string):string =
//        printfn "content for encrypt key: %s" content
        let rsa = new RSACryptoServiceProvider(2048)
        rsa.FromXmlString(privatekey)
//        let rsaParamsPrivate = rsa.ExportParameters(true);
//        rsa.ImportParameters(Para)
        let byteContent = rsa.Decrypt(Convert.FromBase64String(content),true)
        let result = byteContent |> Encoding.UTF8.GetString
//        printfn "decrypt key: %s" result
        result
        
    let HMACSign (message:string,secretKey:string) :string =
//        printfn "secretkey: %s and message %s" secretKey message
        let key = secretKey |>Encoding.UTF8.GetBytes
        let mess = message |>Encoding.UTF8.GetBytes

        let hmacsha256 = new HMACSHA256(key)
        
        let hashmessage = hmacsha256.ComputeHash(mess)
//        printfn "%A" hashmessage
        let result = Convert.ToHexString(hashmessage).ToLowerInvariant()
        result
                                              
        
