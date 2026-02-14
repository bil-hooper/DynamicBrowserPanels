# Contributing to DynamicBrowserPanels

## Development Guidelines

### AI Assistant Interaction

When working with AI assistants (GitHub Copilot, etc.) on this project:

- **Always search the entire solution context** when answering questions
- Proactively use `code_search`, `get_symbols_by_name`, and `get_file` tools to gather comprehensive context
- Don't wait for explicit permission to search - consent is given by default
- Provide answers based on the full codebase understanding, not just open files

### Code Standards

- Target Framework: .NET 8
- C# Version: 12.0
- Follow existing naming conventions and code structure in the solution
- Use XML documentation comments for public APIs
- Handle exceptions appropriately with meaningful error messages

### Project Structure

This is a WinForms application with WebView2 integration for browser functionality.

Key components:
- `Browser\` - Browser tab management and WebView2 controls
- `ImagePad\` - Image pad functionality
- `Main Form\` - Main application form and entry point
- `Dialogs\` - Modal dialogs for user interaction
- `Utilities\` - Shared utility classes

## Testing

- Test all changes with multiple browser tabs
- Verify incognito mode behavior
- Test template save/load functionality
- Verify playlist functionality for both local and online media