# Vibe.UI Demo Application

This is a comprehensive demonstration application showcasing all Vibe.UI components and features.

## Features

- **Component Showcase**: Browse and interact with all 65+ Vibe.UI components
- **Live Examples**: See components in action with real-world examples
- **Code Samples**: View and copy code for each component
- **Theme Switcher**: Toggle between light and dark themes
- **Responsive Design**: Optimized for desktop, tablet, and mobile
- **Interactive Documentation**: Learn by doing with interactive examples

## Running the Demo

```bash
cd samples/Vibe.UI.Demo
dotnet run
```

Then navigate to `https://localhost:5001` in your browser.

## Structure

- **Pages/**: Demo pages for each component category
  - `Input.razor` - Input components (Button, Checkbox, Input, etc.)
  - `Forms.razor` - Form components (Label, Form, etc.)
  - `DataDisplay.razor` - Data display components (Table, Badge, etc.)
  - `Layout.razor` - Layout components (Card, Separator, etc.)
  - `Navigation.razor` - Navigation components (Tabs, Breadcrumb, etc.)
  - `Overlay.razor` - Overlay components (Dialog, Toast, etc.)
  - `Feedback.razor` - Feedback components (Alert, Skeleton, etc.)
  - `Theme.razor` - Theme customization examples

- **Components/**: Custom demo components
  - `ComponentDemo.razor` - Wrapper for displaying component examples
  - `CodePreview.razor` - Code syntax highlighting

## Contributing

If you'd like to add more examples or improve existing ones, please submit a pull request!

## License

MIT
