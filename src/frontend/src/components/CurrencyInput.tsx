import {
  forwardRef,
  useImperativeHandle,
  useLayoutEffect,
  useRef,
  type ChangeEvent,
} from "react";

import {
  digitsFromInput,
  formatMasked,
  valueFromDigits,
} from "@/lib/numberMask";

interface CurrencyInputProps
  extends Omit<
    React.InputHTMLAttributes<HTMLInputElement>,
    "onChange" | "value" | "type" | "inputMode"
  > {
  /** Canonical numeric string, e.g. "3.459". Empty string when blank. */
  value: string;
  onChange: (value: string) => void;
  /** Decimal places the mask fills from the right. 0 for whole numbers (odometer). */
  decimals?: number;
}

/**
 * Numeric input with an ATM-style progressive mask: the user types digits only
 * and the mask fills from the right, placing the decimal point and thousands
 * separators as it goes (typing 3, 4, 5, 9 with decimals=3 reads 0.003, 0.034,
 * 0.345, 3.459). `inputMode="numeric"` gets the plain number pad on mobile
 * instead of the full keyboard `type="number"` brings up on iOS.
 */
const CurrencyInput = forwardRef<HTMLInputElement, CurrencyInputProps>(
  function CurrencyInput(
    { value, onChange, decimals = 2, className, onFocus, onClick, ...props },
    ref
  ) {
    const innerRef = useRef<HTMLInputElement>(null);
    useImperativeHandle(ref, () => innerRef.current as HTMLInputElement);

    const display = formatMasked(value, decimals);

    // Digits enter and leave from the right, so the caret belongs at the end.
    // Runs after every render to undo taps that land mid-number.
    useLayoutEffect(() => {
      const el = innerRef.current;
      if (!el || document.activeElement !== el) return;
      const end = el.value.length;
      if (el.selectionStart !== end || el.selectionEnd !== end) {
        el.setSelectionRange(end, end);
      }
    });

    function handleChange(e: ChangeEvent<HTMLInputElement>) {
      onChange(valueFromDigits(digitsFromInput(e.target.value), decimals));
    }

    function caretToEnd(el: HTMLInputElement) {
      el.setSelectionRange(el.value.length, el.value.length);
    }

    return (
      <input
        ref={innerRef}
        type="text"
        inputMode="numeric"
        autoComplete="off"
        value={display}
        onChange={handleChange}
        onFocus={(e) => {
          caretToEnd(e.currentTarget);
          onFocus?.(e);
        }}
        onClick={(e) => {
          caretToEnd(e.currentTarget);
          onClick?.(e);
        }}
        className={className}
        {...props}
      />
    );
  }
);

export default CurrencyInput;
