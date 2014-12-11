SOVND - Open source streaming music room server

Remember Soundrop.fm?


MQTT Communication spec

	Server published messages
	/{channel}/nowplaying/
		JSON with:
			songid		# ID of currently playing song
			starttime 	# start time of this track (unix time)

	/{channel}/info
		JSON with:
			name
			description
			image
			headerimage
			moderators
	/{channel}/playlist/{songid}
		JSON with:
			addtime  # time this track was added (unix time)
			addedby  # user who added this track
			votetime # time of the first vote on this song (unix time)
			votes    # number of votes
			voters   # users who have voted for this song (JSON array)
			removed  # boolean - if true, this track has been removed
	/{channel}/stats
		/users     # number of active users
		/usernames # names of nonanonymous users in channel
	/{channel}/chat # ATM Username: Message; future: JSON message object

	Future
	/{channel}/toptracks/{1-50} # top tracks, from 1 to 50
	/user/{username}/reauthflag # Client needs to resend login info


	User published messages
	/user/{username}/login
		IP
		Client name + version
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

If you're working on a client where MQTT doesn't function well, raise an issue for adding an alternative transport to the server.

At some point, transports may include: MQTT over websockets, AMQP, XMPP, or others.
