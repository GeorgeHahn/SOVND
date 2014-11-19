SOVND - Open source streaming music room server

Remember Soundrop.fm?


MQTT Communication spec

	Server published messages
	/{channel}/nowplaying/songid # ID of currently playing song
	/{channel}/nowplaying/starttime # ID of currently playing song

	/{channel}/info
		/name
		/description
		/image
		/moderators?
	/{channel}/playlist/{songid}
		/votetime # time of the first vote on this song (TODO: spec time format)
		/votes    # number of votes
		/voters   # users who have voted for this song (TODO: comma delimited?)
		/removed
	/{channel}/stats/users # number of active users
	/{channel}/stats/usernames # names of nonanonymous users in channel
	/{channel}/chat # ATM Username: Message; future: JSON message object

	Future
	/{channel}/toptracks/{1-50} # top tracks, from 1 to 50

	User published messages
	/user/{username}/{channel}/songs/{songid}
		vote	# vote for song
		unvote	# revoke vote for song
		report # Report song
 		remove # Moderator delete
		block  # Moderator block
	/user/{username}/{channel}/chat

	/user/{username}/register/{channel}
		/name
		/description
		/image
		/moderators

TODO:
	- Charlotte: Support dynamically (un/)subscribing to topics
	- Server: Fix song persistence (server should publish with retain flag set)
	- Client: Sort all songs in channel by votes & votetime
	- Client: Set channel
	- Client: Channel browser
	- Client: Login
	- Server: [Moderation] Support muting users
