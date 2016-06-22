Feature: Create review
	In order to run a review
	As a user
	I want to create a new review

Scenario: Create a review with 2 categories
	Given I am logged in
	When I create a new review
		And I fill in a review name
		And I add a category
		And I fill in a category name
		And I fill in a category description
		And I add another category
		And I fill in a category name
		And I fill in a category description
		And I save the review
	Then my new review was created with those categories
		And I am added to the review
		And I am on the "Dashboard"
		And I see the message "Review has been created"

Scenario: Add category to existing Review
	Given I am logged in
		And I own a review
	When I edit my review
		And I add another category
		And I fill in a category name
		And I fill in a category description
		And I save the review
	Then my review is updated with the new category
		And I am on the "Edit review" page for my review
		And I see the message "Review has been saved"

Scenario: Show edit link to review on dashboard
	Given I am logged in
		And I own a review
		And I am on the "Dashboard" page
	When I click on the "Edit review" link of the review
	Then I am on the "Edit review" page for my review

Scenario: Edit review as invited peer
	Given I am logged in
		And I am invited to a review
		And I am on the "Dashboard" page
	When I click on the "Edit review" link of the review
	Then I am on the "Edit review" page for the review

Scenario: Show only Reviews I am part of
	Given I am logged in 
		And I own a review X-Review
		And I am invited to a review Y-Review
		And I am not part of review Z-Review
	When I go to the "Dashboard" page
	Then I see "X-Review"
		And I see "Y-Review"
		And I do not see "Z-Review"

Scenario: Invite new peer to my review
	Given I am logged in
		And I own a review
	When I edit my review
		And I invite a peer
		And I fill in the peer's name
		And I fill in the peer's email address
		And no account exists for that peer's email address
		And I save the review
	Then a new user with the given name and email address was created
		And this user is added to the review

Scenario: Invite existing peer to my review
	Given I am logged in
		And I own a review
	When I edit my review
		And I invite a peer
		And I fill in the peer's name
		And I fill in the peer's email address
		And an account exists for that peer's email address
		And I save the review
	Then this user is added to the review

Scenario: Cannot save review with category without name
	Given I am logged in
		And I own a review
	When I edit my review
		And I add a category
		And I fill in a category description
		And I save the review
	Then I see the message "Please give your category a name"
		And the review is not saved

Scenario: Cannot save review with peer without email
	Given I am logged in
		And I own a review
	When I edit my review
		And I invite a peer
		And I fill in the peer's name
		And I save the review
	Then I see the message "Please enter your peer's email address"
		And the review is not saved

Scenario: Cannot save review with peer without name
	Given I am logged in
		And I own a review
	When I edit my review
		And I invite a peer
		And I fill in the peer's email address
		And I save the review
	Then I see the message "Please enter your peer's name"
		And the review is not saved