import { useCallback, useState } from "react";

interface Props {
  value: string;
  onChange: (value: string) => void;
  onBlur?: () => void;
  name?: string;
  placeholder?: string;
  prefix?: string;
  suffix?: string;
  decimals: number;
  className?: string;
}

/**
 * Controlled text input that restricts to numeric values with a fixed
 * number of decimal places. No spinner, no type="number" quirks.
 * Uses inputMode="decimal" for mobile numeric keyboard.
 */
export function CurrencyInput({
  value,
  onChange,
  onBlur,
  name,
  placeholder,
  prefix,
  suffix,
  decimals,
  className,
}: Props) {
  const [focused, setFocused] = useState(false);

  const handleChange = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => {
      let raw = e.target.value;

      // Allow empty, or a single leading minus for negative (probably not needed here)
      if (raw === "" || raw === ".") {
        onChange(raw);
        return;
      }

      // Strip anything that's not a digit or decimal point
      raw = raw.replace(/[^0-9.]/g, "");

      // Only allow one decimal point
      const parts = raw.split(".");
      if (parts.length > 2) {
        raw = parts[0] + "." + parts.slice(1).join("");
      }

      // Restrict decimal places
      if (parts.length === 2 && parts[1].length > decimals) {
        raw = parts[0] + "." + parts[1].slice(0, decimals);
      }

      onChange(raw);
    },
    [onChange, decimals],
  );

  const handleBlur = useCallback(() => {
    setFocused(false);
    // Format on blur: add trailing zeros if needed
    if (value && value !== "." && !isNaN(Number(value))) {
      const num = parseFloat(value);
      onChange(num.toFixed(decimals));
    }
    onBlur?.();
  }, [value, onChange, decimals, onBlur]);

  return (
    <div className="relative">
      {prefix && (
        <span className="absolute left-2.5 top-1/2 -translate-y-1/2 text-xs text-text-muted">
          {prefix}
        </span>
      )}
      <input
        type="text"
        inputMode="decimal"
        pattern={`[0-9]*\\.?[0-9]{0,${decimals}}`}
        name={name}
        value={value}
        onChange={handleChange}
        onFocus={() => setFocused(true)}
        onBlur={handleBlur}
        placeholder={focused ? "" : placeholder}
        className={`${className ?? "input"} ${prefix ? "pl-6" : ""} ${suffix ? "pr-10" : ""}`}
        autoComplete="off"
        data-bwignore=""
        data-1p-ignore=""
        data-lpignore="true"
        data-form-type="other"
      />
      {suffix && (
        <span className="absolute right-3 top-1/2 -translate-y-1/2 text-xs text-text-muted">
          {suffix}
        </span>
      )}
    </div>
  );
}
