﻿using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace web.Controllers {
	[Route("ws")]
	public class WebSocketController : Controller {

		public string Ip => this.Request.Headers["X-Real-IP"].FirstOrDefault() ?? this.Request.HttpContext.Connection.RemoteIpAddress.ToString();

		/// <summary>
		/// 获取websocket分区
		/// </summary>
		/// <param name="websocketId">本地标识，若无则不传，接口会返回新的，请保存本地localStoregy重复使用</param>
		/// <returns></returns>
		[HttpPost("pre-connect")]
		async public Task<object> preConnect([FromForm] Guid? websocketId) {
			if (websocketId == null) websocketId = Guid.NewGuid();
			var server = WebChatHelper.GetServer(websocketId.Value);
			var token = $"{Guid.NewGuid()}{Guid.NewGuid()}{Guid.NewGuid()}{Guid.NewGuid()}".Replace("-", "");
			await RedisHelper.SetAsync($"webchat_token_{token}", Newtonsoft.Json.JsonConvert.SerializeObject((websocketId, this.Ip)), 10);
			return new {
				code = 0,
				server = $"ws://{server}/ws?token={token}",
				websocketId = websocketId
			};
		}

		/// <summary>
		/// 群聊，获取群列表
		/// </summary>
		/// <returns></returns>
		[HttpPost("get-channels")]
		public object getChannels() {
			return new {
				code = 0,
				channels = WebChatHelper.GetChannels()
			};
		}

		/// <summary>
		/// 群聊，绑定消息频道
		/// </summary>
		/// <param name="websocketId">本地标识，若无则不传，接口会返回，请保存本地重复使用</param>
		/// <param name="channel">消息频道</param>
		/// <returns></returns>
		[HttpPost("subscr-channel")]
		public object subscrChannel([FromForm] Guid? websocketId, [FromForm] string channel) {
			WebChatHelper.Subscribe(websocketId.Value, channel);
			return new {
				code = 0
			};
		}

		/// <summary>
		/// 群聊，发送频道消息，绑定频道的所有人将收到消息
		/// </summary>
		/// <param name="channel">消息频道</param>
		/// <param name="content">发送内容</param>
		/// <returns></returns>
		[HttpPost("send-channelmsg")]
		public object sendChannelmsg([FromForm] string channel, [FromForm] string message) {
			WebChatHelper.Publish(channel, message);
			return new {
				code = 0
			};
		}
		/// <summary>
		/// 单聊
		/// </summary>
		/// <param name="senderWebsocketId">发送者</param>
		/// <param name="receiveWebsocketId">接收者</param>
		/// <param name="message">发送内容</param>
		/// <param name="isReceipt">是否需要回执</param>
		/// <returns></returns>
		[HttpPost("send-msg")]
		public object sendmsg([FromForm] Guid senderWebsocketId, [FromForm] Guid receiveWebsocketId, [FromForm] string message, [FromForm] bool isReceipt = false) {
			WebChatHelper.SendMsg(senderWebsocketId, new[] { receiveWebsocketId }, message, isReceipt);
			return new {
				code = 0
			};
		}
	}
}