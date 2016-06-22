# Team Review

Team Review is a web based tool for improving your team's performance. Using performance reviews, peer reviews and stack ranking.

Check out a live, hosted version of Team Review at <a href="http://teamreview.net">teamreview.net</a>

The project is maintained by a <a href="http://teamaton.com">teamaton</a>.

## Framework 

Team Review is built with **<a href="http://www.asp.net/mvc">asp.net mvc</a>** and the **<a href="https://msdn.microsoft.com/en-us/data/ef.aspx">entity framework</a>**.

Components used:

- <a href="http://sass-lang.com/">sass</a> for css compiling
- bootstrap
- jquery
- html5

We use sass for css compiling, bootstrap for css and javascript components, jquery.

## Get Started

1 Adjust settings in Web.config for smtp-server:
    <mailSettings>
     <smtp>
       <network defaultCredentials="false" host="my-mail.my-server.com" password="password" port="465" enableSsl="true" userName="me@me.com" />
     </smtp>
   </mailSettings>

1 Set default email for automatic messages and as sender:
DefaultContactEmail in EmailService (TeamReview.Core/Services/EmailService.cs)

1 to run the specs, fill in email credentials:
Given I own a google account (in TeamReview.Specs/Steps.cs)

## Copyright & License

Copyright (c) 2013-2016 teamaton UG - Released under the [MIT license](LICENSE).
