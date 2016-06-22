Feature: ShowReviewResults
	In order to get feedback
	As a peer
	I want to see the results of the review

Scenario: Show "Waiting for reviews" for started, provided review if not all peers have provided review
	Given I am logged in
		And I have a started review with 2 categories and 1 additional peers
		And I have provided review
		And I am on the "Dashboard" page
	Then I see "Waiting for reviews" for my review

Scenario: Show "Show results" button for review which I have provided
	Given I am logged in
		And I have a started review with 2 categories and 2 additional peers
		And I have provided review
		And I am on the "Dashboard" page
	When I click on the "Show results" link of the review
	Then I am on the "Results" page for my review

Scenario: Show My Results
	Given I am logged in
		And I have a started review with 2 categories and 2 additional peers
		And all peers have provided the review
		And I am on the "Results" page
	Then for each category I see the peer rating of me (average rating of all peers except mine) and my rating of me		

Scenario: Show Team results
	Given I am logged in
		And I have a started review with 2 categories and 2 additional peers
		And all peers have provided the review
		And I am on the "Results" page
	Then for each category and each peer I see their peer rating (average rating of all peers except his/hers)		

Scenario: Show Stack Ranking Results
	Given I am logged in
		And I have a started review with 2 categories and 2 additional peers
		And all peers have provided the review
		And I am on the "Results" page
	Then for each peer I see their stacked peer rating (average rating of all peers except his/hers)
		And for each peer I see the sum of their stacked peer ratings

Scenario: Show Peers That have completed review
	Given I am logged in
		And I have a started review with 2 categories and 2 additional peers Isabelle, Eva
		And Eva has completed the review
		And Isabelle has not completed the review
		And I am on the "Results" page
	Then I see that Eva has completed the review
		And I see that Isabelle has not completed review
