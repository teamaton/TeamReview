Feature: Test HtmlUnit WebDriver implementation
	In order to quickly execute spec tests
	As a developer
	I want to use HtmlUnit as my browser

Scenario: Open the TeamReview homepage
	Given I navigate to the homepage
	Then I should find <h1> on page