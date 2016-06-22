Feature: Create and edit review
	In order to run a review
	As a user
	I want to create and edit a review

Scenario: Create a review with 2 categories
	Given I am a registered user and logged in
	When I create a new review
		| Name        | Length in weeks | Category 1 Name | Category 1 description      | Category 2 Name | Category 2 description       |
		| FirstReview | 4               | Category One    | This is the first category. | Category Two    | This is the second category. |
	Then my new review was created with those categories
		And I am added to the review
		And I am on the Dashboard
		And I see the message "Review has been created"

Scenario: Delete category from existing review
	Given I have a review with categories Speed and Performance
	When I delete category Speed
	Then the review has only category Performance

Scenario: Delete peer from existing review
	Given I have a review with peers Mel and Jeb
	When I delete peer Mel
	Then the review has only peers Jeb