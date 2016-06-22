Feature: Register and login
	In order to create a review
	As a user
	I want to register an account

Scenario: Register using Google account
	Given I own a Google account
		And I am not logged into TeamReview
	When I register a new account
		And I use my Google account
		And I fill in a user name
		And I finish registering
	Then a new account was created with my Google address
		And I am logged in
		And I am on the "Dashboard"

Scenario: Standard Registration
	Given I am not logged into TeamReview
	When I register a new account
		And I fill in my email address "test@teamreview.net"
		And I finish registering
	Then I see the message "An email has been send to test@teamreview.net. Please check your inbox for further instructions."
		#And an email is sent to test@teamreview.net with a hard to guess validation url

Scenario: Validate Email and enter UserName and Password
	Given I registered standardly
	When I follow link in validation email
		And I enter UserName and Password twice
		And I finish registering
	Then I am on the "Dashboard"

Scenario: Standard Login
	Given I have a standard account with email
		And I am not logged into TeamReview
	When I go to the "Login" page
		And I enter UserName and Password
		And I finish login
	Then I am logged in
		And I am on the "Dashboard"

# not yet needed
Scenario: I have lost my Password
	Given I have a standard account with email
		And I am not logged into TeamReview
	When I go to the "Login" page
		And I say that I have lost my password
		And I enter my email address "test@teamreview.net"
		And I request an email
	Then I see the message "An email has been send to test@teamreview.net. Please check your inbox for further instructions."
		#And an email is sent to test@teamreview.net with a hard to guess new password url

#Scenario: Change UserName

#Scenario: Change Password

Scenario: Login as existing user using Google account
	Given I have an account at TeamReview
		And I am not logged into TeamReview
	When I log in using my Google account
	Then I am logged in
		And I am on the "Dashboard"

Scenario: Login as non-existing user using Google account
	Given I own a Google account
		And I don't have an account at TeamReview
	When I register using my Google account
		And I finish registering
	Then a new account was created with my Google address
		And I am logged in
		And I am on the "Dashboard"

Scenario: Log out of TeamReview
	Given I am logged into TeamReview
	When I log out
	Then I am not logged into TeamReview
		And I am on the login page