import { forwardRef, type ChangeEvent } from "react";

interface CurrencyInputProps
  extends Omit<React.InputHTMLAttributes<HTMLInputElement>, "onChange" | "value"> {
  value: string;
  onChange: (value: string) => void;
  decimals?: number;
}

const CurrencyInput = forwardRef<HTMLInputElement, CurrencyInputProps>(
  function CurrencyInput({ value, onChange, decimals = 2, className, ...props }, ref) {
    function handleChange(e: ChangeEvent<HTMLInputElement>) {
      const raw = e.target.value;
      // Allow empty, digits, and a single decimal point with up to N decimal places
      const regex = new RegExp(`^\\d*\\.?\\d{0,${decimals}}$`);
      if (raw === "" || regex.test(raw)) {
        onChange(raw);
      }
    }

    return (
      <input
        ref={ref}
        type="text"
        inputMode="decimal"
        value={value}
        onChange={handleChange}
        className={className}
        {...props}
      />
    );
  }
);

export default CurrencyInput;
