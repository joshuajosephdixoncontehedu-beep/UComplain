import { ShieldCheck } from "lucide-react";

export default function AuthLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex min-h-screen w-full">
      <div className="hidden w-[42%] max-w-md flex-col justify-between bg-sidebar px-10 py-12 lg:flex">
        <div className="flex items-center gap-2.5">
          <div className="flex size-9 items-center justify-center rounded-lg bg-sidebar-primary text-sidebar-primary-foreground">
            <ShieldCheck className="size-5" aria-hidden="true" />
          </div>
          <div className="flex flex-col leading-none">
            <span className="text-sm font-semibold text-white">UComplain</span>
            <span className="text-[11px] text-sidebar-foreground/70">Admin Portal</span>
          </div>
        </div>

        <div className="flex flex-col gap-3">
          <h1 className="text-2xl leading-snug font-semibold text-white">
            UComplain
          </h1>
          <p className="text-sm leading-relaxed text-sidebar-foreground/80">
            Every report is verified by a human before it reaches the operational queue.
            Sign in to review, assign, and resolve cases across your team.
          </p>
        </div>

        <p className="text-xs text-sidebar-foreground/50">
          UComplain — Admin Portal
        </p>
      </div>

      <div className="flex flex-1 items-center justify-center bg-slate-100 px-4 py-12">
        {children}
      </div>
    </div>
  );
}
