# HelloAgent

This project uses code from agent-framework-dotnet but uses nuget packages and not project references.
It compiles easely with Visual Studio 2026.

The DevUI project was changed to use "/v1/responses" and not "/v1/chat/completions". This allows to use gpt-6-luna. 
At runtime I had this problem :
"Function tools with reasoningeffort are not supported for gpt-6-luna in /v1/chat/completions. To use function tools, use /v1/responses or set reasoningeffort to 'none'."
