import {
  BarChart3,
  FileText,
  Home,
  ScrollText,
  Settings,
  ShieldAlert,
  Tag,
  UserCog,
  Users,
  type LucideIcon,
} from "lucide-react";
import { isReviewerOrAbove, isSuperAdmin } from "@/lib/auth/permissions";
import type { AdminRole } from "@/types/enums";

export interface NavItem {
  label: string;
  href: string;
  icon: LucideIcon;
  visible?: (role: AdminRole) => boolean;
}

export const navItems: NavItem[] = [
  { label: "Dashboard", href: "/dashboard", icon: Home },
  { label: "Reports", href: "/reports", icon: FileText },
  { label: "Verification", href: "/verification", icon: ShieldAlert, visible: isReviewerOrAbove },
  { label: "Users", href: "/users", icon: Users },
  { label: "Categories", href: "/categories", icon: Tag },
  { label: "Analytics", href: "/analytics", icon: BarChart3 },
  { label: "Administrators", href: "/administrators", icon: UserCog, visible: isSuperAdmin },
  { label: "Audit Logs", href: "/audit-logs", icon: ScrollText, visible: isSuperAdmin },
  { label: "Settings", href: "/settings", icon: Settings, visible: isSuperAdmin },
];
