# Domain

WorldCuppy is a **2026 FIFA World Cup prediction game**. Users predict knockout match outcomes and score points.

## Key Concepts

- **Tournament** — the 2026 World Cup. 48 teams, 12 groups of 4; top 2 + 8 best third-place teams advance to a Round of 32.
- **Match** — fixture with two teams, a kickoff time, and an optional final score.
- **Prediction** — a user's predicted scoreline for a knockout match, locked before kickoff.
- **User** — registered player who submits predictions and accumulates points.
- **Leaderboard** — users ranked by total points.

## Prediction Scope

Knockout stage only: Round of 32 → Round of 16 → Quarter-finals → Semi-finals → Final.

## Scoring

| Outcome | Points |
|---|---|
| Exact scoreline | 3 pts |
| Correct result, wrong score | 1 pt |
| Wrong result | 0 pts |

Points are awarded automatically when a match result is recorded.

## Entity Names

Use consistently: `Tournament`, `Team`, `Match`, `Prediction`, `User`, `Leaderboard`, `MatchResult`, `KnockoutRound`
