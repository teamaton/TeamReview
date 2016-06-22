Feature: Review reports
	In order to analyze review results
	As a user
	I want to see reports for me, my peers and my team

Background: 
	Given I have a review with peers Mel and Jeb
		And the review has categories performance and productivity
		And the following assessments have been made
		| Reviewer | ReviewedPeer | PerformanceRound1 | ProductivityRound1 | PerformanceRound2 | ProductivityRound2 |
		| (me)     | (me)         | 4                 | 4                  | 5                 | 5                  |
		| (me)     | Jeb          | 4                 | 4                  | 5                 | 5                  |
		| (me)     | Mel          | 6                 | 4                  | 9                 | 5                  |
		| Jeb      | (me)         | 2                 | 6                  | 4                 | 6                  |
		| Jeb      | Jeb          | 2                 | 6                  | 5                 | 6                  |
		| Jeb      | Mel          | 3                 | 6                  | 7                 | 8                  |
		| Mel      | (me)         | 3                 | 5                  | 6                 | 7                  |
		| Mel      | Jeb          | 3                 | 5                  | 5                 | 7                  |
		| Mel      | Mel          | 3                 | 5                  | 5                 | 5                  |

Scenario: Peer report for one category and one peer 	
	When I create a report for peers
		And I choose Jeb for peer and productivity for category
	Then I get a peer report containing productivity data for Jeb

Scenario: Peer report for one category and two peers
	When I create a report for peers
		And I choose Jeb and Mel for peer and performance for category
	Then I get a peer report containing performance data for Jeb and Mel

Scenario: My own category ratings report compared with ratings of me by the team
	When I create a report for myself
		And I choose 'performance' for category
	Then I get a performance report with my own ratings compared with ratings of me by the team

Scenario: My own stack ratings report compared with ratings of me by the team
	When I create a stack report for myself
	Then I get a stack ratings report with my own ratings compared with ratings of me by the team

Scenario: Team report for one category
	When I create a team report
		And I choose 'productivity' for category
	Then I get a team report with productivity data for my team

Scenario: Team report with stack ratings for all peers over all categories
	When I create a stack report for my team		
	Then I get a stack ratings report with summed ratings for each peer