﻿using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using OpenPOP.MIME;
using OpenPOP.MIME.Header;
using OpenPOP.POP3;

namespace OpenPOPUnitTests.POP3
{
	[TestFixture]
	public class POPClientPositiveTests
	{
		/// <summary>
		/// This test comes from the RFC 1939 example located at 
		/// http://tools.ietf.org/html/rfc1939#page-16
		/// </summary>
		[Test]
		public void TestAPOPAuthentication()
		{
			const string welcomeMessage = "+OK POP3 server ready <1896.697170952@dbc.mtview.ca.us>";
			const string loginMessage = "+OK mrose's maildrop has 2 messages (320 octets)";
			const string serverResponses = welcomeMessage + "\r\n" + loginMessage + "\r\n";
			StringReader reader = new StringReader(serverResponses);

			StringBuilder popClientCommands = new StringBuilder();
			StringWriter writer = new StringWriter(popClientCommands);

			POPClient client = new POPClient();
			client.Connect(reader, writer);

			// The POPClient should now have seen, that the server supports APOP
			Assert.IsTrue(client.APOPSupported);

			client.Authenticate("mrose", "tanstaaf", AuthenticationMethod.APOP);

			const string expectedOutput = "APOP mrose c4c9334bac560ecc979e58001b3e22fb\r\n";
			string output = popClientCommands.ToString();

			// The correct APOP command should have been sent
			Assert.AreEqual(expectedOutput, output);
		}

		/// <summary>
		/// http://tools.ietf.org/html/rfc1939#page-6
		/// </summary>
		[Test]
		public void TestGetMessageCount()
		{
			const string welcomeMessage = "+OK";
			const string okUsername = "+OK";
			const string okPassword = "+OK";
			const string statCommandResponse = "+OK 5 10"; // 5 Messages with total size of 10 octets
			const string serverResponses = welcomeMessage + "\r\n" + okUsername + "\r\n" + okPassword + "\r\n" + statCommandResponse + "\r\n";
			StringReader reader = new StringReader(serverResponses);

			StringWriter writer = new StringWriter(new StringBuilder());

			POPClient client = new POPClient();
			client.Connect(reader, writer);
			client.Authenticate("test", "test");

			int numberOfMessages = client.GetMessageCount();

			// We expected 5 messages
			Assert.AreEqual(5, numberOfMessages);
		}

		/// <summary>
		/// http://tools.ietf.org/html/rfc1939#page-8
		/// </summary>
		[Test]
		public void TestDeleteMessage()
		{
			const string welcomeMessage = "+OK";
			const string okUsername = "+OK";
			const string okPassword = "+OK";
			const string DeleteResponse = "+OK"; // Message was deleted
			const string QuitAccepted = "+OK";
			const string serverResponses = welcomeMessage + "\r\n" + okUsername + "\r\n" + okPassword + "\r\n" + DeleteResponse + "\r\n" + QuitAccepted + "\r\n";
			StringReader reader = new StringReader(serverResponses);

			StringBuilder popClientCommands = new StringBuilder();
			StringWriter writer = new StringWriter(popClientCommands);

			POPClient client = new POPClient();
			client.Connect(reader, writer);
			client.Authenticate("test", "test");

			client.DeleteMessage(5);

			const string expectedOutput = "DELE 5";
			string output = getLastCommand(popClientCommands);

			// We expected that the last command is the delete command
			Assert.AreEqual(expectedOutput, output);

			client.Disconnect();

			const string expectedOutputAfterQuit = "QUIT";
			string outputAfterQuit = getLastCommand(popClientCommands);

			// We now expect that the client has sent the QUIT command
			Assert.AreEqual(expectedOutputAfterQuit, outputAfterQuit);
		}

		/// <summary>
		/// http://tools.ietf.org/html/rfc1939#page-8
		/// </summary>
		[Test]
		public void TestDeleteAllMessages()
		{
			const string welcomeMessage = "+OK";
			const string okUsername = "+OK";
			const string okPassword = "+OK";
			const string messageCountResponse = "+OK 2 5"; // 2 messages with total size of 5 octets
			const string DeleteResponse = "+OK"; // Message was deleted
			const string QuitAccepted = "+OK";
			const string serverResponses = welcomeMessage + "\r\n" + okUsername + "\r\n" + okPassword + "\r\n" + messageCountResponse  + "\r\n" + DeleteResponse + "\r\n" + DeleteResponse + "\r\n" + QuitAccepted + "\r\n";
			StringReader reader = new StringReader(serverResponses);

			StringBuilder popClientCommands = new StringBuilder();
			StringWriter writer = new StringWriter(popClientCommands);

			POPClient client = new POPClient();
			client.Connect(reader, writer);
			client.Authenticate("test", "test");

			// Delete all the messages
			client.DeleteAllMessages();

			// Check that message 1 and message 2 was deleted
			string[] commandsFired = getCommands(popClientCommands);

			bool message1Deleted = false;
			bool message2Deleted = false;
			foreach (string commandFired in commandsFired)
			{
				if (commandFired.Equals("DELE 1"))
					message1Deleted = true;

				if (commandFired.Equals("DELE 2"))
					message2Deleted = true;
			}

			// We expect message 1 to be deleted
			Assert.IsTrue(message1Deleted);

			// We expect message 2 to be deleted
			Assert.IsTrue(message2Deleted);

			// Quit and commit
			client.Disconnect();

			const string expectedOutputAfterQuit = "QUIT";
			string outputAfterQuit = getLastCommand(popClientCommands);

			// We now expect that the client has sent the QUIT command
			Assert.AreEqual(expectedOutputAfterQuit, outputAfterQuit);
		}

		/// <summary>
		/// http://tools.ietf.org/html/rfc1939#page-5
		/// </summary>
		[Test]
		public void TestQuit()
		{
			const string welcomeMessage = "+OK";
			const string okUsername = "+OK";
			const string okPassword = "+OK";
			const string quitOK = "+OK";
			const string serverResponses = welcomeMessage + "\r\n" + okUsername + "\r\n" + okPassword + "\r\n" + quitOK + "\r\n";
			StringReader reader = new StringReader(serverResponses);

			StringBuilder popClientCommands = new StringBuilder();
			StringWriter writer = new StringWriter(popClientCommands);

			POPClient client = new POPClient();
			client.Connect(reader, writer);
			client.Authenticate("test", "test");

			client.QUIT();

			// Get the last command issued by the client
			string output = getLastCommand(popClientCommands);

			// We expect it to be QUIT
			const string expectedOutput = "QUIT";

			Assert.AreEqual(expectedOutput, output);
		}

		/// <summary>
		/// http://tools.ietf.org/html/rfc1939#page-9
		/// </summary>
		[Test]
		public void TestNoOperation()
		{
			const string welcomeMessage = "+OK";
			const string okUsername = "+OK";
			const string okPassword = "+OK";
			const string noopOK = "+OK";
			const string serverResponses = welcomeMessage + "\r\n" + okUsername + "\r\n" + okPassword + "\r\n" + noopOK + "\r\n";
			StringReader reader = new StringReader(serverResponses);

			StringBuilder popClientCommands = new StringBuilder();
			StringWriter writer = new StringWriter(popClientCommands);

			POPClient client = new POPClient();
			client.Connect(reader, writer);
			client.Authenticate("test", "test");

			client.NOOP();

			// Get the last command issued by the client
			string output = getLastCommand(popClientCommands);

			// We expect it to be NOOP
			const string expectedOutput = "NOOP";

			Assert.AreEqual(expectedOutput, output);
		}

		/// <summary>
		/// http://tools.ietf.org/html/rfc1939#page-9
		/// </summary>
		[Test]
		public void TestReset()
		{
			const string welcomeMessage = "+OK";
			const string okUsername = "+OK";
			const string okPassword = "+OK";
			const string rsetOK = "+OK";
			const string serverResponses = welcomeMessage + "\r\n" + okUsername + "\r\n" + okPassword + "\r\n" + rsetOK + "\r\n";
			StringReader reader = new StringReader(serverResponses);

			StringBuilder popClientCommands = new StringBuilder();
			StringWriter writer = new StringWriter(popClientCommands);

			POPClient client = new POPClient();
			client.Connect(reader, writer);
			client.Authenticate("test", "test");

			client.RSET();

			// Get the last command issued by the client
			string output = getLastCommand(popClientCommands);

			// We expect it to be RSET
			const string expectedOutput = "RSET";

			Assert.AreEqual(expectedOutput, output);
		}

		/// <summary>
		/// http://tools.ietf.org/html/rfc1939#page-12
		/// </summary>
		[Test]
		public void TestGetMessageUID()
		{
			const string welcomeMessage = "+OK";
			const string okUsername = "+OK";
			const string okPassword = "+OK";
			const string messageUidResponse = "+OK 2 psycho"; // Message 2 has UID psycho
			const string serverResponses = welcomeMessage + "\r\n" + okUsername + "\r\n" + okPassword + "\r\n" + messageUidResponse + "\r\n";
			StringReader reader = new StringReader(serverResponses);

			StringBuilder popClientCommands = new StringBuilder();
			StringWriter writer = new StringWriter(popClientCommands);

			POPClient client = new POPClient();
			client.Connect(reader, writer);
			client.Authenticate("test", "test");

			const string expectedOutput = "psycho";

			// Delete all the messages
			string output = client.GetMessageUID(2);

			// We now expect that the client has given us the correct UID
			Assert.AreEqual(expectedOutput, output);
		}

		/// <summary>
		/// http://tools.ietf.org/html/rfc1939#page-12
		/// </summary>
		[Test]
		public void TestGetMessageUIDs()
		{
			const string welcomeMessage = "+OK";
			const string okUsername = "+OK";
			const string okPassword = "+OK";
			const string messageUidAccepted = "+OK";
			const string messageUid1 = "1 psycho"; // Message 1 has UID psycho
			const string messageUid2 = "2 lord"; // Message 2 has UID lord
			const string uidListEnded = ".";
			const string serverResponses = welcomeMessage + "\r\n" + okUsername + "\r\n" + okPassword + "\r\n" + messageUidAccepted + "\r\n" + messageUid1 + "\r\n" + messageUid2 + "\r\n" + uidListEnded + "\r\n";
			StringReader reader = new StringReader(serverResponses);

			StringWriter writer = new StringWriter(new StringBuilder());

			POPClient client = new POPClient();
			client.Connect(reader, writer);
			client.Authenticate("test", "test");

			// Get the UIDs for all the messages in sorted order from 1 and upwards
			System.Collections.Generic.List<string> uids = client.GetMessageUIDs();

			// The list should have size 2
			Assert.AreEqual(2, uids.Count);

			// The first entry should have uid psycho
			Assert.AreEqual("psycho", uids[0]);

			// The second entry should have uid lord
			Assert.AreEqual("lord", uids[1]);
		}

		/// <summary>
		/// http://tools.ietf.org/html/rfc1939#page-7
		/// </summary>
		[Test]
		public void TestGetMessageSize()
		{
			const string welcomeMessage = "+OK";
			const string okUsername = "+OK";
			const string okPassword = "+OK";
			const string messageSize = "+OK 9 200"; // Message 9 has size 200 octets
			const string serverResponses = welcomeMessage + "\r\n" + okUsername + "\r\n" + okPassword + "\r\n" + messageSize + "\r\n";
			StringReader reader = new StringReader(serverResponses);

			StringWriter writer = new StringWriter(new StringBuilder());

			POPClient client = new POPClient();
			client.Connect(reader, writer);
			client.Authenticate("test", "test");

			// Message 9 should have size 200
			const int expectedOutput = 200;
			int output = client.GetMessageSize(9);

			Assert.AreEqual(expectedOutput, output);
		}

		/// <summary>
		/// http://tools.ietf.org/html/rfc1939#page-7
		/// </summary>
		[Test]
		public void TestGetMessageSizes()
		{
			const string welcomeMessage = "+OK";
			const string okUsername = "+OK";
			const string okPassword = "+OK";
			const string messageListAccepted = "+OK 2 messages (320 octets)";
			const string messageSize1 = "1 120";
			const string messageSize2 = "2 200";
			const string messageListEnd = ".";
			const string serverResponses = welcomeMessage + "\r\n" + okUsername + "\r\n" + okPassword + "\r\n" + messageListAccepted + "\r\n" + messageSize1 + "\r\n" + messageSize2 + "\r\n" + messageListEnd + "\r\n";
			StringReader reader = new StringReader(serverResponses);

			StringWriter writer = new StringWriter(new StringBuilder());

			POPClient client = new POPClient();
			client.Connect(reader, writer);
			client.Authenticate("test", "test");

			// Message 9 should have size 200
			System.Collections.Generic.List<int> messageSizes = client.GetMessageSizes();

			// The list should have size 2
			Assert.AreEqual(2, messageSizes.Count);

			// The first entry should have size 120
			Assert.AreEqual(120, messageSizes[0]);

			// The second entry should have size 200
			Assert.AreEqual(200, messageSizes[1]);
		}

		/// <summary>
		/// http://tools.ietf.org/html/rfc1939#page-8
		/// This also tests that the message parsing is correct
		/// </summary>
		[Test]
		public void TestGetMessageNoContentType()
		{
			const string welcomeMessage = "+OK";
			const string okUsername = "+OK";
			const string okPassword = "+OK";
			const string okMessageFetch = "+OK";
			const string messageHeaders = "Return-path: <test@test.com>\r\nEnvelope-to: test2@test.com\r\nDelivery-date: Tue, 05 Oct 2010 04:02:06 +0200\r\nReceived: from test by test.com with local (MailThing 4.69)\r\n\t(envelope-from <test@test.com>)\r\n\tid 1P2wr0-0003vw-U9\r\n\tfor test2@test.com; Tue, 05 Oct 2010 04:02:06 +0200\r\nTo: test2@test.com\r\nSubject: CRON-APT completed on test-server [/etc/auto-apt/configuration]\r\nMessage-Id: <E1P2wr0-0003vw-U9@test.com>\r\nFrom: test <test@test.com>\r\nDate: Tue, 05 Oct 2010 04:02:06 +0200";
			const string messageHeaderToBodyDelimiter = "";
			const string messageBody = "CRON-APT RUN [/etc/auto-apt/configuration]: Tue Oct  5 04:00:01 CEST 2010\r\nCRON-APT SLEEP: 116, Tue Oct  5 04:01:57 CEST 2010\r\nCRON-APT ACTION: 3-download\r\nCRON-APT LINE: /user/bin/apt-get dist-upgrade -d -y -o APT::Get::Show-Upgraded=true\r\nReading package lists...\r\nBuilding dependency tree...\r\nReading state information...\r\nThe following packages will be upgraded:\r\n  libaprutil1 libfreetype6\r\n2 upgraded, 0 newly installed, 0 to remove and 0 not upgraded.\r\nNeed to get 445kB of archives.\r\nAfter this operation, 4096B of additional disk space will be used.\r\nGet:1 http://security.debian.org Lenny/updates/main libaprutil1 1.2.12+DFSG-8+lenny5 [74.0kB]\r\nGet:2 http://security.debian.org Lenny/updates/main libfreetype6 2.3.7-2+lenny4 [371kB]\r\nFetched 445kB in 0s (1022kB/s)\r\nDownload complete and in download only mode\r\n";
			const string messageEnd = ".";

			const string serverResponses = welcomeMessage + "\r\n" + okUsername + "\r\n" + okPassword + "\r\n" + okMessageFetch + "\r\n" + messageHeaders + "\r\n" + messageHeaderToBodyDelimiter + "\r\n"  + messageBody + "\r\n" + messageEnd + "\r\n";

			StringReader reader = new StringReader(serverResponses);

			StringWriter writer = new StringWriter(new StringBuilder());

			POPClient client = new POPClient();
			client.Connect(reader, writer);
			client.Authenticate("test", "test");

			Message message = client.GetMessage(132);

			Assert.NotNull(message);
			Assert.NotNull(message.Headers);
			
			Assert.AreEqual("CRON-APT completed on test-server [/etc/auto-apt/configuration]", message.Headers.Subject);
			Assert.AreEqual("E1P2wr0-0003vw-U9@test.com", message.Headers.MessageID);

			// The Date header was:
			// Date: Tue, 05 Oct 2010 04:02:06 +0200
			// The +0200 is the same as going back two hours in UTC
			Assert.AreEqual(new DateTime(2010, 10, 5, 2, 2, 6, DateTimeKind.Utc), message.Headers.DateSent);
			Assert.AreEqual("Tue, 05 Oct 2010 04:02:06 +0200", message.Headers.Date);

			Assert.NotNull(message.Headers.From);
			Assert.AreEqual("test@test.com", message.Headers.From.Address);
			Assert.AreEqual("test", message.Headers.From.DisplayName);

			// There should only be one receiver
			Assert.NotNull(message.Headers.To);
			Assert.AreEqual(1, message.Headers.To.Count);
			Assert.AreEqual("test2@test.com", message.Headers.To[0].Address);
			Assert.IsEmpty(message.Headers.To[0].DisplayName);

			Assert.NotNull(message.Headers.ReturnPath);
			Assert.AreEqual("test@test.com", message.Headers.ReturnPath.Address);
			Assert.IsEmpty(message.Headers.ReturnPath.DisplayName);

			// There should only be one body
			Assert.NotNull(message.MessageBody);
			Assert.AreEqual(1, message.MessageBody.Count);
			Assert.AreEqual(messageBody, message.MessageBody[0].Body);
			// Even though there is no declaration saying the type is text/plain, this is the default if nothing is supplied
			Assert.AreEqual("text/plain", message.MessageBody[0].Type);
		}

		/// <summary>
		/// Tests a real email between Kasper and John
		/// </summary>
		[Test]
		public void TestGetMessageIso88591()
		{
			const string welcomeMessage = "+OK";
			const string okUsername = "+OK";
			const string okPassword = "+OK";
			const string okMessageFetch = "+OK";
			const string messageHeaders = "Return-Path: <nhojmc@spam.gmail.com>\r\nReceived: from fep22 ([80.160.76.226]) by fep31.mail.dk\r\n          (InterMail vM.7.09.02.02 201-2219-117-103-20090326) with ESMTP\r\n          id <20101018210945.WKYX19924.fep31.mail.dk@fep22>\r\n          for <thefeds@spam.mail.dk>; Mon, 18 Oct 2010 23:09:45 +0200\r\nX-TDC-Received-From-IP: 74.125.82.54\r\nX-TDCICM: v=1.1 cv=f/0wZEcxj9tnJ8pax90Ax24drQNytfp8yOyhRTrlZkQ= c=1 sm=1 a=8nJEP1OIZ-IA:10 a=AP5iSteITFkr-SbgjHQA:9 a=ev7MReTRFKXz7Gsr7Zgb0CdRMZcA:4 a=wPNLvfGTeEIA:10 a=1PqlE-0FaytecYBE:21 a=bZoohpooc2ldffvA:21 a=rxAKiTD8bQmauZVlwM9vwA==:117\r\nX-TDC-RCPTTO: thefeds@spam.mail.dk\r\nX-TDC-FROM: nhojmc@spam.gmail.com\r\nReceived: from [74.125.82.54] ([74.125.82.54:63778] helo=mail-ww0-f54.google.com)\r\n\tby fep22 (envelope-from <nhojmc@spam.gmail.com>)\r\n\t(ecelerity 2.2.2.45 r()) with ESMTP\r\n\tid 92/2D-17911-897BCBC4; Mon, 18 Oct 2010 23:09:45 +0200\r\nReceived: by wwb39 with SMTP id 39so482967wwb.35\r\n        for <thefeds@spam.mail.dk>; Mon, 18 Oct 2010 14:09:42 -0700 (PDT)\r\nDKIM-Signature: v=1; a=rsa-sha256; c=relaxed/relaxed;\r\n        d=gmail.com; s=gamma;\r\n        h=domainkey-signature:mime-version:received:received:date:message-id\r\n         :subject:from:to:content-type;\r\n        bh=Xbyk5CmNRvc3U3s+wNmr55cx9fVqL9C82Dw3trI+OUA=;\r\n        b=CguczhTSNbLI1IOWFbFoExmIMnJPoU54mQUD7GyP7uK3B6dzews4jWP60jvWVmq/15\r\n         cmE9f08W2hLMsI6VtLtbPsOq/WVjVRK9A0sikvyCCxDdBy141Al94Ef0fAwt77Fc7jLW\r\n         YWLuM5PNjxjNjsw4D6pVvhfRcLArERrhrCXxw=\r\nDomainKey-Signature: a=rsa-sha1; c=nofws;\r\n        d=gmail.com; s=gamma;\r\n        h=mime-version:date:message-id:subject:from:to:content-type;\r\n        b=jVEyGC3V7FnaxCiZyHtOuPTe5goCPTIqdbJZ3TE/k1mAaS/gaQwHdJJrZNH1Zqi81+\r\n         kHAI86Z+o/raYZM51gdzhBg7DcuN2FgLnfnlncbAtNDQxR/CadLsL/OFKBg2CpgszGXA\r\n         vlMPszRP7C658j5v38dM8J4p6Q86nAnem7v6g=\r\nMIME-Version: 1.0\r\nReceived: by 10.227.145.70 with SMTP id c6mr1938128wbv.106.1287436181523; Mon,\r\n 18 Oct 2010 14:09:41 -0700 (PDT)\r\nReceived: by 10.227.146.13 with HTTP; Mon, 18 Oct 2010 14:09:41 -0700 (PDT)\r\nDate: Mon, 18 Oct 2010 17:09:41 -0400\r\nMessage-ID: <AANLkTik0O_9JZCeS7Za__w_G6L=9jKq2=BQKnqHVXAQo@mail.gmail.com>\r\nSubject: Email Addresses\r\nFrom: John McDaniel <nhojmc@spam.gmail.com>\r\nTo: Kasper Foens <thefeds@spam.mail.dk>\r\nContent-Type: text/plain; charset=ISO-8859-1";
			const string messageHeaderToBodyDelimiter = "";
			const string messageBody = "I have run into an issue with the email addresses. It seems one of the\r\ncases my QA dept ran across is when the Email address is not of the\r\nform x@y. I fixed the case where the code would throw an exception on\r\nan invalid email, but I see where I am going to need access to the\r\noriginal address strings.  I\'m hesitant to add properties to the\r\nMessageHeader such as RawTo, RawFrom  but I really don\'t see a better\r\nway of getting at that information. Any thoughts? Maybe keeping the\r\nheader items in a publicly accessible dictionary???\r\n\r\nJohn\r\n";
			const string messageEnd = ".";

			const string serverResponses = welcomeMessage + "\r\n" + okUsername + "\r\n" + okPassword + "\r\n" + okMessageFetch + "\r\n" + messageHeaders + "\r\n" + messageHeaderToBodyDelimiter + "\r\n" + messageBody + "\r\n" + messageEnd + "\r\n";

			StringReader reader = new StringReader(serverResponses);

			StringWriter writer = new StringWriter(new StringBuilder());

			POPClient client = new POPClient();
			client.Connect(reader, writer);
			client.Authenticate("something", "else");

			Message message = client.GetMessage(132);

			Assert.NotNull(message);
			Assert.NotNull(message.Headers);

			Assert.AreEqual("Email Addresses", message.Headers.Subject);
			Assert.AreEqual("AANLkTik0O_9JZCeS7Za__w_G6L=9jKq2=BQKnqHVXAQo@mail.gmail.com", message.Headers.MessageID);

			Assert.AreEqual("1.0", message.Headers.MimeVersion);

			// Testing a custom header
			Assert.NotNull(message.Headers.UnknownHeaders);
			string[] tdcHeader = message.Headers.UnknownHeaders.GetValues("X-TDC-Received-From-IP");
			Assert.NotNull(tdcHeader);

			// This is to stop content assist from nagging me. It is clear that it is not null now
			// since the Assert above would have failed then
			if (tdcHeader != null)
			{
				Assert.AreEqual(1, tdcHeader.Length);
				Assert.AreEqual("74.125.82.54", tdcHeader[0]);
			}

			Assert.NotNull(message.Headers.ContentType);
			Assert.NotNull(message.Headers.ContentType.CharSet);
			Assert.AreEqual("ISO-8859-1", message.Headers.ContentType.CharSet);
			Assert.NotNull(message.Headers.ContentType.MediaType);
			Assert.AreEqual("text/plain", message.Headers.ContentType.MediaType);

			// The Date header was:
			// Date: Mon, 18 Oct 2010 17:09:41 -0400
			// The -0400 is the same as adding 4 hours in UTC
			Assert.AreEqual(new DateTime(2010, 10, 18, 21, 9, 41, DateTimeKind.Utc), message.Headers.DateSent);
			Assert.AreEqual("Mon, 18 Oct 2010 17:09:41 -0400", message.Headers.Date);

			Assert.NotNull(message.Headers.From);
			Assert.AreEqual("nhojmc@spam.gmail.com", message.Headers.From.Address);
			Assert.AreEqual("John McDaniel", message.Headers.From.DisplayName);

			// There should only be one receiver
			Assert.NotNull(message.Headers.To);
			Assert.AreEqual(1, message.Headers.To.Count);
			Assert.AreEqual("thefeds@spam.mail.dk", message.Headers.To[0].Address);
			Assert.AreEqual("Kasper Foens", message.Headers.To[0].DisplayName);

			Assert.NotNull(message.Headers.ReturnPath);
			Assert.AreEqual("nhojmc@spam.gmail.com", message.Headers.ReturnPath.Address);
			Assert.IsEmpty(message.Headers.ReturnPath.DisplayName);

			// There should only be one body
			Assert.NotNull(message.MessageBody);
			Assert.AreEqual(1, message.MessageBody.Count);
			Assert.AreEqual(messageBody, message.MessageBody[0].Body);
			Assert.AreEqual("text/plain", message.MessageBody[0].Type);
		}

		/// <summary>
		/// Base64 string from http://en.wikipedia.org/wiki/Base64#Examples
		/// </summary>
		[Test]
		public void TestGetMessageBase64()
		{
			const string welcomeMessage = "+OK";
			const string okUsername = "+OK";
			const string okPassword = "+OK";
			const string okMessageFetch = "+OK";
			const string messageHeaders = "Return-Path: <thefeds@spam.mail.dk>\r\nMessage-ID: <4CBACC87.8080600@mail.dk>\r\nDate: Sun, 17 Oct 2010 12:14:31 +0200\r\nFrom: =?ISO-8859-1?Q?Kasper_F=F8ns?= <thefeds@spam.mail.dk>\r\nMIME-Version: 1.0\r\nTo: =?ISO-8859-1?Q?Kasper_F=F8ns?= <thefeds@spam.mail.dk>\r\nSubject: Test =?ISO-8859-1?Q?=E6=F8=E5=C6=D8=C5?=\r\nContent-Type: text/plain; charset=US-ASCII;\r\nContent-Transfer-Encoding: base64";
			const string messageHeaderToBodyDelimiter = "";
			// Removed last K for the \n and added == for padding
			const string messageBody = "TWFuIGlzIGRpc3Rpbmd1aXNoZWQsIG5vdCBvbmx5IGJ5IGhpcyByZWFzb24sIGJ1dCBieSB0aGlz\r\nIHNpbmd1bGFyIHBhc3Npb24gZnJvbSBvdGhlciBhbmltYWxzLCB3aGljaCBpcyBhIGx1c3Qgb2Yg\r\ndGhlIG1pbmQsIHRoYXQgYnkgYSBwZXJzZXZlcmFuY2Ugb2YgZGVsaWdodCBpbiB0aGUgY29udGlu\r\ndWVkIGFuZCBpbmRlZmF0aWdhYmxlIGdlbmVyYXRpb24gb2Yga25vd2xlZGdlLCBleGNlZWRzIHRo\r\nZSBzaG9ydCB2ZWhlbWVuY2Ugb2YgYW55IGNhcm5hbCBwbGVhc3VyZS4==";
			const string messageEnd = ".";

			const string serverResponses = welcomeMessage + "\r\n" + okUsername + "\r\n" + okPassword + "\r\n" + okMessageFetch + "\r\n" + messageHeaders + "\r\n" + messageHeaderToBodyDelimiter + "\r\n" + messageBody + "\r\n" + messageEnd + "\r\n";

			StringReader reader = new StringReader(serverResponses);

			StringWriter writer = new StringWriter(new StringBuilder());

			POPClient client = new POPClient();
			client.Connect(reader, writer);
			client.Authenticate("something", "else");

			Message message = client.GetMessage(132);

			Assert.NotNull(message);
			Assert.NotNull(message.Headers);

			Assert.AreEqual("Test æøåÆØÅ", message.Headers.Subject);
			Assert.AreEqual("4CBACC87.8080600@mail.dk", message.Headers.MessageID);

			Assert.AreEqual("1.0", message.Headers.MimeVersion);

			Assert.NotNull(message.Headers.ContentType);
			Assert.NotNull(message.Headers.ContentType.CharSet);
			Assert.AreEqual("US-ASCII", message.Headers.ContentType.CharSet);
			Assert.NotNull(message.Headers.ContentType.MediaType);
			Assert.AreEqual("text/plain", message.Headers.ContentType.MediaType);

			// We are using base64 as encoding
			Assert.AreEqual(ContentTransferEncoding.Base64, message.Headers.ContentTransferEncoding);

			// The Date header was:
			// Date: Sun, 17 Oct 2010 12:14:31 +0200
			// The +0200 is the same as substracting 2 hours in UTC
			Assert.AreEqual(new DateTime(2010, 10, 17, 10, 14, 31, DateTimeKind.Utc), message.Headers.DateSent);
			Assert.AreEqual("Sun, 17 Oct 2010 12:14:31 +0200", message.Headers.Date);

			Assert.NotNull(message.Headers.From);
			Assert.AreEqual("thefeds@spam.mail.dk", message.Headers.From.Address);
			Assert.AreEqual("Kasper Føns", message.Headers.From.DisplayName);

			// There should only be one receiver
			Assert.NotNull(message.Headers.To);
			Assert.AreEqual(1, message.Headers.To.Count);
			Assert.AreEqual("thefeds@spam.mail.dk", message.Headers.To[0].Address);
			Assert.AreEqual("Kasper Føns", message.Headers.To[0].DisplayName);

			Assert.NotNull(message.Headers.ReturnPath);
			Assert.AreEqual("thefeds@spam.mail.dk", message.Headers.ReturnPath.Address);
			Assert.IsEmpty(message.Headers.ReturnPath.DisplayName);

			const string expectedOutput = "Man is distinguished, not only by his reason, but by this singular passion from other animals, which is a lust of the mind, that by a perseverance of delight in the continued and indefatigable generation of knowledge, exceeds the short vehemence of any carnal pleasure.";

			// There should only be one body
			Assert.NotNull(message.MessageBody);
			Assert.AreEqual(1, message.MessageBody.Count);
			Assert.AreEqual(expectedOutput, message.MessageBody[0].Body);
			Assert.AreEqual("text/plain", message.MessageBody[0].Type);
		}
		
		/// <summary>
		/// http://tools.ietf.org/html/rfc1939#page-11
		/// </summary>
		[Test]
		public void TestGetMessageHeaders()
		{
			const string welcomeMessage = "+OK";
			const string okUsername = "+OK";
			const string okPassword = "+OK";
			const string messageTopAccepted = "+OK";
			const string messageHeaders = "Subject: [Blinded by the lights] New Comment On: Comparison of .Net libraries for fetching emails via POP3\r\n";
			const string messageHeaderToBodyDelimiter = ""; // Blank line ends message
			const string messageListingEnd = ".";
			const string serverResponses = welcomeMessage + "\r\n" + okUsername + "\r\n" + okPassword + "\r\n" + messageTopAccepted + "\r\n" + messageHeaders + "\r\n" + messageHeaderToBodyDelimiter + "\r\n" + messageListingEnd + "\r\n";
			StringReader reader = new StringReader(serverResponses);

			StringWriter writer = new StringWriter(new StringBuilder());

			POPClient client = new POPClient();
			client.Connect(reader, writer);
			client.Authenticate("test", "test");

			// Fetch the header of message 7
			MessageHeader header = client.GetMessageHeaders(7);

			const string expectedSubject = "[Blinded by the lights] New Comment On: Comparison of .Net libraries for fetching emails via POP3";
			string subject = header.Subject;

			Assert.AreEqual(expectedSubject, subject);
		}

		/// <summary>
		/// Helper method to get the last line from a <see cref="StringBuilder"/>
		/// which is the last line that the client has sent.
		/// </summary>
		/// <param name="builder">The builder to get the last line from</param>
		/// <returns>A single line, which is the last one in the builder</returns>
		private static string getLastCommand(StringBuilder builder)
		{
			string[] commands = getCommands(builder);
			return commands[commands.Length - 1];
		}

		/// <summary>
		/// Helper method to get a string array of the commands issued by a client.
		/// </summary>
		/// <param name="builder">The builder to get the commands from</param>
		/// <returns>A string array where each entry is a command</returns>
		private static string[] getCommands(StringBuilder builder)
		{
			return builder.ToString().Split(new[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries);
		}
	}
}