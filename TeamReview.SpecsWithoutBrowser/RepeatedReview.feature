Feature: RepeatedReview
	In order to show changes over time
	As a user
	I want to fill in the same review regularly

#Scenario: Send out invitation emails for second round of review
#	Given I have a review with peers Kai and Anton
#		And I have started the review one week ago
#	When the program checks for new rounds
#	Then a new round is started
#		And Kai and Anton receive an email with an invitation link to the new round of review

Scenario: Allow and provide second feedback of review
	Given I have a review with peers Kai and Anton
		And I have started the review one week ago
	When the program checks for new rounds
	Then a new round is started
		And I can provide a second feedback of review
	When I provide data for the second round of review
	Then my feedback is saved