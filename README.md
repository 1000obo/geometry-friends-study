# When Should I Lead or Follow? Understanding Initiative Levels in Human-AI Collaborative Gameplay

## Abstract
<i>Dynamics in Human-AI interaction should lead to more satisfying and engaging collaboration. Key open questions are how to design
such interactions and the role personal goals and expectations play. We developed three AI partners of varying initiative (leader,
follower, shifting) in a collaborative game called Geometry Friends. We conducted a within-subjects experiment with 60 participants
to assess personal AI partner preference and performance satisfaction as well as perceived warmth and competence of AI partners.
Results show that AI partners following human initiative are perceived as warmer and more collaborative. However, some participants
preferred AI leaders for their independence and speed, despite being seen as less friendly. This suggests that assigning a leadership role
to the AI partner may be suitable for time-sensitive scenarios. We identify design factors to consider when developing collaborative AI
agents with varying levels of initiative to create more effective human-AI teams that consider context and individual preference.</i>

## Introduction
This paper proposes using game environments like Geometry Friends to evaluate the interaction between humans
and artificial agents. To better investigate the effects of AI initiative levels on human perception, we designed three
different agent behaviours within a collaborative game:
- a **leader agent** who acts according to its own plan expecting
the human player to follow
- a **follower agent** that aligns its actions to follow the human player’s plan
- a **shifting agent** that changes its initiative depending on whether the human player follows its plan.
  
For all conditions, the agent will play as the circle character of the game, while the human will play as the rectangle.

Given the level of initiative of the agent, including its willingness to shift initiative, we intend to answer four research
questions:
- **RQ1**: How does an agent’s initiative influence the perception of AI partners (agent focus e.g., perceived agent
warmth and competence, social identification)?
- **RQ2**: How does an agent’s initiative impact the perceived quality of collaboration (interaction focus e.g.,
satisfaction with team and agent performance, objective performance)?
- **RQ3**: How does an agent’s initiative impact the humans’ self-perception (user focus e.g., satisfaction with
self-performance, perception of played role)?
- **RQ4**: How does an agent’s initiative affect the overall team perception (team focus e.g., agent preference)?

To address these questions, we conducted a study with a mixed-methods design, using the collaborative game Geometry
Friends, involving 60 participants across three countries. We present our rationales for designing AI agents with varying
levels of initiative, as well as an empirical evaluation of interacting with these agents to assess perceived AI partner
warmth and competence, social identification with the team, satisfaction with performance (self, AI partner, and team),
and AI partner personal preference based on different levels of initiative.


## Form
Participants will fill out the form in the form folder.
## Downloading the Project
To download the project, simply clone the repository or download the project ZIP file.

## Running the Game
To run the game, follow these steps:
1. Enter the exe folder
2. Inside the folder, you may need to unlock the files in the Agents folder to run the agents.
3. Run the game executable, GeometryFriends.exe, and connect the game controller.
4. In the game options, select the "human + circle AI" option in single player mode.
5. Choose the desired agents implementation: `L` for leader, `F` for follower, or `S` for the agent that shifts initiative between leader and follower. There are 6 different order options: 
   * L, F, S
   * L, S, F
   * F, L, S
   * F, S, L
   * S, F, L
   * S, L, F
6. To start playing, simply click the "play!" button.
   * For the training part, players will play the training levels. 
   * For the first agent, they will play the first level set for 5 minutes.
   * For the second agent, they will play the second level set.
   * For the third agent, they will play the third level set. The level sets are balanced and introduce the same game challenges.

## Log Files
You can find the generated log files of the experiment, including screen recordings and game logs, in the logs folder of the exe folder.

## More Information
To cite this paper, please use:
Inês Lobo, Janin Koch, Jennifer Renoux, Inês Batina, and Rui Prada. 2024. When Should I Lead or Follow: Understanding Initiative Levels in Human-AI Collaborative Gameplay. In Proceedings of the 2024 ACM Designing Interactive Systems Conference (DIS '24). Association for Computing Machinery, New York, NY, USA, 2037–2056. https://doi.org/10.1145/3643834.3661583
