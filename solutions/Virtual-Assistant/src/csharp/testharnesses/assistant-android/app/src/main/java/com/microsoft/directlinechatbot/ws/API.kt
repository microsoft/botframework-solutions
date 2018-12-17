// The MIT License (MIT)
//
// Copyright (c) 2018 Smart&Soft
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

package com.microsoft.directlinechatbot.ws

import com.microsoft.directlinechatbot.bo.Id
import com.microsoft.directlinechatbot.bo.Message
import com.microsoft.directlinechatbot.bo.StartConversation
import retrofit2.Call
import retrofit2.http.*

/**
 * @author David Fournier
 * @since 2018.03.05
 */
internal interface API
{

  @POST("conversations")
  fun startConversation(@Header("Authorization") secret: String): Call<StartConversation>

  @POST("conversations/{conversationId}/activities")
  fun send(@Body message: Message, @Path("conversationId") conversationId: String, @Header("Authorization") secret: String): Call<Id>

}