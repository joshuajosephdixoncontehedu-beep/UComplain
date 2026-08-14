interface TimelineProps<T> {
  items: T[];
  keyExtractor: (item: T) => string;
  renderItem: (item: T) => React.ReactNode;
  emptyLabel: string;
}

/** Shared connector-line timeline shell, parametrized so each history type supplies its own row content. */
export function Timeline<T>({ items, keyExtractor, renderItem, emptyLabel }: TimelineProps<T>) {
  if (items.length === 0) {
    return <p className="py-6 text-center text-sm text-muted-foreground">{emptyLabel}</p>;
  }

  return (
    <ol className="flex flex-col">
      {items.map((item, index) => (
        <li key={keyExtractor(item)} className="relative flex gap-3 pb-4 last:pb-0">
          {index < items.length - 1 && (
            <span className="absolute top-5 left-[9px] h-full w-px bg-border" aria-hidden="true" />
          )}
          <span className="mt-0.5 size-[18px] shrink-0 rounded-full bg-muted" aria-hidden="true" />
          <div className="min-w-0 flex-1">{renderItem(item)}</div>
        </li>
      ))}
    </ol>
  );
}
