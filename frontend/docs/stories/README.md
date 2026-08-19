# Истории (stories)

Одна история = одна задача = один изолированный запуск агента-разработчика.

Файл называется `<эпик>.<номер>-<краткое-имя>.md`, например `2.1-onboarding-photos.md`.

Жизненный цикл:

```
draft → approved → in-progress → review → done
```

- `draft` — создана `bmad-sm`, ещё не проверена человеком.
- `approved` — человек согласовал, можно брать в работу.
- `in-progress` — `bmad-dev` пишет код.
- `review` — код готов, `bmad-qa` проверяет.
- `done` — QA принял, `npm run check` зелёный.

Шаблон — [`TEMPLATE.md`](TEMPLATE.md).
