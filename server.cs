echo("-------------------------------------------- loading squarebracket shit");
exec("./jettison.cs");
exec("./Support_TCPClient.cs");

// some of the code (mainly listener-related stuff) comes from conan's farming server script so big thanks to the developers of that

function sendMessage(%cl, %msg, %type)
{
	if ($Pref::Server::sbchatkey $= "")
	{
		talk("You fucked up.");
		error("ERROR: $Pref::Server::sbchatkey is missing!");
		return;
	}
	
	%msg = urlEnc(%msg);
	%author = urlEnc(%cl.name);
	%bl_id = urlEnc(%cl.bl_id);
	%key = urlEnc($Pref::Server::sbchatkey);
	%type = urlEnc(%type);

	%query = "author=" @ %author @ "&message=" @ %msg @ "&bl_id=" @ %bl_id @ "&verifykey=" @ %key @ "&type=" @ %type;
	%tcp = TCPClient("POST", "127.0.0.1", "28010", "/rcvmsg", %query, "", "sbMessager");
}

function sbMessager::buildRequest(%this)
{
	%len = strLen(%this.query);
	%path = %this.path;

	if(%len)
	{
		%type		= "Content-Type: text/html; charset=windows-1252\r\n";
		if(%this.method $= "GET" || %this.method $= "HEAD")
		{
			%path	= %path @ "?" @ %this.query;
		}
		else
		{
			%length	= "Content-Length:" SPC %len @ "\r\n";
			%body	= %this.query;
		}
	}
	%requestLine	= %this.method SPC %path SPC %this.protocol @ "\r\n";
	%host			= "Host:" SPC %this.server @ "\r\n";
	%ua				= "User-Agent: Torque/1.3\r\n";
	%request = %requestLine @ %host @ %ua @ %length @ %type @ "\r\n" @ %body;
	return %request;
}

//

function createSbListener()
{
	if ($Pref::Server::sbchatkey $= "") 
	{
		talk("You fucked up.");
		error("ERROR: $Pref::Server::sbchatkey is missing!");
		return;
	}
	
	if(isObject($SbMessageListener))
	{
		talk("Listener already exists!");
		return;
	}
	
	$SbMessageListener = new TCPObject(SbMessageListenerClass){};
	$SbMessageListener.schedule(8000, listen, 28008);
	
	echo("squareBracket listener running on port" SPC 28008);
}

function SbMessageListenerClass::onConnectRequest(%this, %ip, %id)
{
	if (getSubStr(%ip, 0, strPos(%ip, ":")) !$= "127.0.0.1") //ignoring connection attempts from other servers
	{
		return;
	}
	if(isObject(%this.connection[%ip]))
	{
		echo(%this.getName() @ ": Got duplicate connection from" SPC %ip);
		%this.connection[%ip].disconnect();
		%this.connection[%ip].delete();
	}
	echo(%this.getName() @ ": Creating connection to" SPC %ip);
	%this.connection[%ip] = new TCPobject("sbListenClient", %id){class = SbMessageListenerClass; parent = %this; client = %ip;};
	%this.connection[%ip].schedule(180000, delete);
}

function sbListenClient::onLine(%this, %line)
{
	//if ($debugDiscordListener)
	//{
	//talk("[" @ strReplace(%line, "\t", "|") @ "]");
	jettisonParse(%line);
	%messageShit = $JSON::Value;
	//}
	//else if (getFieldCount(%line) > 2)
	echo(%messageShit);

	%message = %messageShit.get("message");
	
	if (%message !$= "") {
		%username = %messageShit.get("username");

		// the sbchat protocol appends "Discord-" to any messages sent from discord, so we can easily figure out if
		// the message is from discord or from squarebracket directly.
		if (strPos(%username, "Discord-") == 0) {
			%cleanUsername = getSubStr(%username, 8, strlen(%username) - 8);
			messageAll('', "<color:5865F2>(Discord) " @ %cleanUsername @ "\c6: " @ %message);
		} else {
			messageAll('', "<color:FF8A00>(squareBracket) " @ %username @ "\c6: " @ %message);
		}
	} else {
		%system = %messageShit.get("system");
		talk(%system);
	}

	if (getFieldCount(%line) > 2)
	{
		//%key = getField(%line, 0);
		//if (strPos(%key, $Pref::Server::server2blkey) != 0)
		//{
		//	error("ERROR: sbListenClient::onLine - key mismatch!");
		//	return;
		//}
		messageAll('', "<color:7289DA>@" @ getField(%line, 1) @ "\c6: " @ getFields(%line, 2, 200));
		echo("[DISCORD] @" @ getField(%line, 1) @ "\c6: " @ getFields(%line, 2, 200));
	}
}

package SquareBracket
{
	function serverCmdMessageSent(%client, %message)
	{
		sendMessage(%client, %message, "message");
		parent::serverCmdMessageSent(%client, %message);
	}
};

schedule(100, 0, createSbListener);
activatePackage(SquareBracket);