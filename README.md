Start time: Mid-day 21st of July
End time: Mid-day 23rd of July

Notes:
- I started this project by first creating an agentic harness for game development in Unity. I prioritized small scope demo development at high quality, since that is the sort of project I was assigned.

- The harness is not the currently popular looping design where the development and testing is handled all by the agent, but instead runs on a Waterfall-like philosophy where every feature pass is additive and must be subjected to human review before the next feature can be tackled.

- I have utilised this "Human-in-the-loop" design philosophy to develop the project demo from the ground up, the evidence of which can be seen in the 40 feature reports in the Docs directory

- I have used only a single asset pack (and a few extra UI sprites) to have a coherent gameplay look.

- I have made the grid and unit sizes easily adjustable. The demo-as-is fits the mockup sketch's layout in the doc, and can be further adjusted (I have kept it as 20x12 to keep the testing area small, but this is a data driven value that can be adjusted in the grid definition scriptable object)

- The unit tests were all authored and evaluated automatically by the harness, as I find that AI tools are best suited for these sorts of automated deterministic-validation tasks such as automated unit tests.

- The brief has some slightly ambigious wording regarding certain mechanics and design decisions, so I have used my best judgement on those (See DEVELOPMENT_REPORT in Docs)

- There are still a number of improvements that can be made to this project, but I have tried not to go too out of scope engineering wise for something done in 2 days as a small demonstration.

- I am happy to speak on any design or architectural decision made here, as well as their trade offs and what would be picked in a full scope feature rich project. Otherwise, feel free to look at the agentic documentation shipped with this project

- The working windows build will be forwarded via e-mail alongside this repo's link, as I did not wish to commit it here. Please let me know if there are any issues.

Author: Alper Sarı
23/07/2026