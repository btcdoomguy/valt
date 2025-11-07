### Novo Modal

- Criar uma nova Avalonia Window
- Ir no code-behind e herdar de ValtBaseWindow
- Criar uma nova ViewModel herdando de ValtModalValidatorViewModel ou ValtModalViewModel
- No XAML, registrar o namespace do ViewModel e o DataType
- Registrar o viewmodel como Transient e também registrar no factory method