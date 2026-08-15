import Image from "next/image";
import { cn } from "@/lib/utils";

interface LogoProps {
  /** Pixel size of the square badge (both width and height). */
  size?: number;
  className?: string;
}

/**
 * The UComplain mark, rendered in a white rounded badge so it stays legible on both
 * the dark sidebar/auth-panel background and light card backgrounds. Source image is
 * public/ucomplain-icon.png (cropped/padded from the supplied brand artwork).
 */
export function Logo({ size = 32, className }: LogoProps) {
  return (
    <div
      className={cn("flex shrink-0 items-center justify-center overflow-hidden rounded-md bg-white", className)}
      style={{ width: size, height: size }}
    >
      <Image
        src="/ucomplain-icon.png"
        alt="UComplain"
        width={size}
        height={size}
        className="h-full w-full object-contain p-0.5"
        priority
      />
    </div>
  );
}
