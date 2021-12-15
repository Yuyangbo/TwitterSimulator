namespace TwitterWeb
open System
open System.Collections.Generic
open Suave
open Suave.WebSocket

module MessageType =
    type MessageInActor =
        |TweetContents of WebSocket*string
        |TweetBySubscriber of WebSocket*string*string
        |TweetToMention of WebSocket*string*string
        |AddSubscribe of WebSocket*string
//        |SubscribeUser of WebSocket*string
//        |GetSubscribeTweets of string
//        |GetTagTweets of WebSocket*string*string
//        |GetMentionTweets of WebSocket*string*string
//        |ReTweets of WebSocket*string*string
        
    type MessageInSocket = {
            mutable Content : string[]
            mutable status : int
            mutable alerts: string
    }
    
    type userInfomation = {
                       mutable id:string
                       mutable live:bool
                       mutable password :string
                       mutable tweets:System.Collections.Generic.List<string>
                       mutable subscript:System.Collections.Generic.List<string>
                       mutable follower:System.Collections.Generic.List<string>
    }

    type Register = {
            Name: string
            Password: string
    }

    type LoginUser = {
            Name: string
            Password: string
            HMACname: string
    }
    
    type LogoutUser = {
            Name: string
            HMACname: string
    }

    type Tweet = {
            Name: string
            TweetContent: string
            HMACname: string
    }
    
    type ReceiveMessage = {
            types:string
            content:string
            HMACname: string
    }
    
    type AllTweets = {
            Name: string
            HMACname: string
    }
    
    type TagTweets = {
            Name:string
            Tag: string
            HMACname: string
    }
    
    type MentionTweets = {
            MentionName: string
            HMACname: string
    }
    
    type SubscribeUser = {
            Subscriber:string
            Follower:string
            HMACname: string
    }
    
    type Retweet = {
            Name: string
            TweetContent:string
            HMACname: string
    }
    
    type PublicKeyRequest = {
            Name: string
    }
    
   type KeyPair = {
           mutable publicKey:string
           mutable privateKey:string
   }

