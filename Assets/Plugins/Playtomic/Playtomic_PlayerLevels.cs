/* Playtomic Unity3d API
-----------------------------------------------------------------------
 Documentation is available at: 
 	https://playtomic.com/api/unity

 Support is available at:
 	https://playtomic.com/community 
 	https://playtomic.com/issues
 	https://playtomic.com/support has more options if you're a premium user

 Github repositories:
 	https://github.com/playtomic

You may modify this SDK if you wish but be kind to our servers.  Be
careful about modifying the analytics stuff as it may give you 
borked reports.

Pull requests are welcome if you spot a bug or know a more efficient
way to implement something.

Copyright (c) 2011 Playtomic Inc.  Playtomic APIs and SDKs are licensed 
under the MIT license.  Certain portions may come from 3rd parties and 
carry their own licensing terms and are referenced where applicable.
*/ 
#define WWW_SUPPORT
#if UNITY_FLASH
#undef WWW_SUPPORT
#endif
#if WWW_SUPPORT

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class Playtomic_PlayerLevels : Playtomic_Responder
{	
	public Playtomic_PlayerLevels () { }
	
	private static string SECTION;
	private static string SAVE;
	private static string LIST;
	private static string LOAD;
	private static string RATE;
	
	internal static void Initialise(string apikey)
	{
		SECTION = Playtomic_Encode.MD5("playerlevels-" + apikey);
		RATE = Playtomic_Encode.MD5("playerlevels-rate-" + apikey);
		LIST = Playtomic_Encode.MD5("playerlevels-list-" + apikey);
		SAVE = Playtomic_Encode.MD5("playerlevels-save-" + apikey);
		LOAD = Playtomic_Encode.MD5("playerlevels-load-" + apikey);
	}
	
	public IEnumerator Save(Playtomic_PlayerLevel level)
	{
		var postdata = new Dictionary<String, String>();
		postdata.Add("data", level.Data);
		postdata.Add("playerid", level.PlayerId);
		postdata.Add("playername", level.PlayerName);
		postdata.Add("playersource", Playtomic.SourceUrl);
		postdata.Add("name", level.Name);
		postdata.Add("nothumb", "y");
		postdata.Add("customfields", level.CustomData.Count.ToString());
		
		var n = 0;
		
		foreach(var key in level.CustomData.Keys)
		{
			postdata.Add("ckey" + n, key);
			postdata.Add("cdata" + n, level.CustomData[key]);
			n++;
		}
		
		string url;
		WWWForm post;
		
		Playtomic_Request.Prepare(SECTION, SAVE, postdata, out url, out post);
		
		WWW www = new WWW(url, post);
		yield return www;
		
		var response = Playtomic_Request.Process(www);
	
		if (response.Success)
		{
			var data = (Dictionary<string,object>)response.JSON;

			foreach(KeyValuePair<string,object> kvp in data)
			{
				var name = WWW.UnEscapeURL(kvp.Key);
				var value = WWW.UnEscapeURL((string)kvp.Value);
				response.Data.Add(name, value);
			}
		}
		
		SetResponse(response, "Save");
	}
	
	public IEnumerator Load(string levelid)
	{
		var postdata = new Dictionary<String, String>();
		postdata.Add("levelid", levelid);
		
		string url;
		WWWForm post;
		
		Playtomic_Request.Prepare(SECTION, LOAD, postdata, out url, out post);
		
		WWW www = new WWW(url, post);
		yield return www;
		
		var response = Playtomic_Request.Process(www);
	
		if (response.Success)
		{
			var item = response.JSON;
			var level = new Playtomic_PlayerLevel();
			level.LevelId = (string)item["LevelId"];
			level.PlayerSource = (string)item["PlayerSource"];
			level.PlayerName = WWW.UnEscapeURL((string)item["PlayerName"]);
			level.Name = WWW.UnEscapeURL((string)item["Name"]);
			level.Votes = (int)(double)item["Votes"];
			level.Plays = (int)(double)item["Plays"];
			level.Rating = (double)item["Rating"];
			level.Score = (int)(double)item["Score"];
			level.SDate = DateTime.Parse((string)item["SDate"]);
			level.RDate = WWW.UnEscapeURL((string)item["RDate"]);
			
			if(item.ContainsKey("Data"))
				level.Data = (string)item["Data"];
			
			if(item.ContainsKey("CustomData"))
			{
				Dictionary<string, object> customdata = (Dictionary<string,object>)item["CustomData"];
	
				foreach(KeyValuePair<string,object> kvp in customdata)
					level.CustomData.Add(kvp.Key, WWW.UnEscapeURL((string)kvp.Value));
			}
			
			response.Levels = new List<Playtomic_PlayerLevel>();
			response.Levels.Add(level);
		}
		
		SetResponse(response, "Load");
	}

	public IEnumerator List(string mode, int page, int perpage)
	{
		return List(mode, page, perpage, false, false, new Dictionary<String, String>(), DateTime.MinValue, DateTime.MaxValue);
	}
	
	public IEnumerator List(string mode, int page, int perpage, bool includedata, bool includethumbs)
	{
		return List(mode, page, perpage, includedata, includethumbs, new Dictionary<String, String>(), DateTime.MinValue, DateTime.MaxValue);
	}
		
	public IEnumerator List(string mode, int page, int perpage, bool includedata, bool includethumbs, DateTime datemin, DateTime datemax)
	{
		return List(mode, page, perpage, includedata, includethumbs, new Dictionary<String, String>(), datemin, datemax);
	}
	
	public IEnumerator List(string mode, int page, int perpage, bool includedata, bool includethumbs, Hashtable customdatahashtable, DateTime datemin, DateTime datemax)
	{
		var dict = new Dictionary<String, String>();
		
		foreach(var key in customdatahashtable.Keys)
		{
			dict.Add(key.ToString(), customdatahashtable[key].ToString());
		}
		
		return List(mode, page, perpage, includedata, includethumbs, dict, datemin, datemax);
	}
	
	public IEnumerator List(string mode, int page, int perpage, bool includedata, bool includethumbs, Dictionary<string,string> customdata, DateTime datemin, DateTime datemax)
	{
		var postdata = new Dictionary<String, String>();
		postdata.Add("mode", mode);
		postdata.Add("page", page.ToString());
		postdata.Add("perpage", perpage.ToString());
		postdata.Add("thumbs", includethumbs ? "y" : "n");
		postdata.Add("data", includedata ? "y" : "n");
		postdata.Add("datemin", datemin.ToString("MM/dd/yyyy"));
		postdata.Add("datemax", datemax.ToString("MM/dd/yyyy"));
		
		if(customdata != null)
		{
			var n = 0;
		
			foreach(var key in customdata.Keys)
			{
				postdata.Add("ckey" + n, key);
				postdata.Add("cdata" + n, customdata[key]);
				n++;
			}
			
			postdata.Add("filters", customdata.Count.ToString());
		}
		else
		{
			postdata.Add("filters", "0");
		}
			
		string url;
		WWWForm post;
		
		Playtomic_Request.Prepare(SECTION, LIST, postdata, out url, out post);
		
		WWW www = new WWW(url, post);
		yield return www;
		
		var response = Playtomic_Request.Process(www);
	
		if (response.Success)
		{
			var data = (Dictionary<string,object>)response.JSON;
			var levels = (List<object>)data["Levels"];
			var len = levels.Count;
			
			response.NumItems = (int)(double)data["NumLevels"];
			response.Levels = new List<Playtomic_PlayerLevel>();
			
			for(var i=0; i<len; i++)
			{
				Dictionary<string,object> item = (Dictionary<string,object>)levels[i];	
				
				var level = new Playtomic_PlayerLevel();
				level.LevelId = (string)item["LevelId"];
				level.PlayerSource = (string)item["PlayerSource"];
				level.PlayerName = WWW.UnEscapeURL((string)item["PlayerName"]);
				level.Name = WWW.UnEscapeURL((string)item["Name"]);
				level.Votes = (int)(double)item["Votes"];
				level.Plays = (int)(double)item["Plays"];
				level.Rating = (double)item["Rating"];
				level.Score = (int)(double)item["Score"];
				level.SDate = DateTime.Parse((string)item["SDate"]);
				level.RDate = WWW.UnEscapeURL((string)item["RDate"]);
				
				if(item.ContainsKey("Data"))
					level.Data = (string)item["Data"];
				
				if(item.ContainsKey("CustomData"))
				{
					Dictionary<string,object> cd = (Dictionary<string,object>)item["CustomData"];
	
					foreach(KeyValuePair<string,object> kvp in cd)
						level.CustomData.Add((string)kvp.Key, WWW.UnEscapeURL((string)kvp.Value));
				}
				
				response.Levels.Add(level);
			}
		}
		
		SetResponse(response, "List");
	}
	
	public IEnumerator Rate(string levelid, int rating)
	{
		if(rating < 1 || rating > 10)
		{
			SetResponse(Playtomic_Response.Error(401), "Rate");
			yield break;
		}

		var postdata = new Dictionary<String, String>();
		postdata.Add("levelid", levelid);
		postdata.Add("rating", rating.ToString());
		
		string url;
		WWWForm post;
		
		Playtomic_Request.Prepare(SECTION, RATE, postdata, out url, out post);
		
		WWW www = new WWW(url, post);
		yield return www;
		
		var response = Playtomic_Request.Process(www);
		SetResponse(response, "Rate");
	}
	
	public void LogStart(string levelid)
	{
		Playtomic.Log.PlayerLevelStart(levelid);
	}
	
	public void LogQuit(string levelid)
	{
		Playtomic.Log.PlayerLevelQuit(levelid);
	}
	
	public void LogWin(string levelid)
	{
		Playtomic.Log.PlayerLevelWin(levelid);
	}
	
	public void LogRetry(string levelid)
	{
		Playtomic.Log.PlayerLevelRetry(levelid);
	}
	
	public void Flag(string levelid)
	{
		Playtomic.Log.PlayerLevelFlag(levelid);
	}
}
#endif