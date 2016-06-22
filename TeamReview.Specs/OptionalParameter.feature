Feature: Test optional parameters
	In order to cover different test cases
	As a spec writer
	I want to specify one or more arguments

Scenario: Optional argument
	Given I have one argument aaa
	Given I have one argument "a a a"
	And I have one argument aaa and another argument bbb
