# Copilot Repository Instructions

## Git Safety Rules
- Copilot must never push to origin.
- Copilot must never create pull requests unless explicitly asked.

## Forbidden Actions
- Auto-push.
- Create branches without explicit instruction.

## Execution Time Limit
- If any operation or tool batch takes more than 30 seconds, pause further work and ask the user whether to continue. Wait for their answer before continuing, retrying, or starting another step.
- Set terminal timeouts to at most 30,000 milliseconds and keep explicit waits within that budget. If a command continues in the background after timing out, ask before taking further action; do not poll it automatically.
- Apply this limit to all tools, including Git/MCP calls and parallel batches, not just terminal commands. Prefer bounded tools; if a potentially slow tool cannot return within 30 seconds, ask before invoking it.
- A progress update is not permission to continue beyond the limit.

## Coding Style Rules
- Use brace-less style for single-action `if` blocks.
- Keep braces for multi-statement `if` blocks.
