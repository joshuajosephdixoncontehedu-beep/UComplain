import Svg, { Circle, Path, Rect } from 'react-native-svg';

const BRAND = '#1D4ED8';
const BRAND_TINT = '#DBEAFE';
const BORDER = '#E2E8F0';
const MUTED = '#CBD5E1';
const INK = '#1E293B';
const AMBER = '#F59E0B';

/**
 * Custom flat-style illustrations for the three onboarding screens — built directly as
 * react-native-svg shapes rather than downloaded third-party art, so there's no
 * licensing/attribution question and the palette matches the design system exactly.
 */

export function ReportIllustration() {
  return (
    <Svg width={200} height={200} viewBox="0 0 200 200">
      <Circle cx={100} cy={100} r={90} fill={BRAND_TINT} />

      <Rect x={50} y={55} width={100} height={110} rx={14} fill="#FFFFFF" stroke={BORDER} strokeWidth={1.5} />
      <Rect x={64} y={69} width={72} height={46} rx={8} fill={BRAND_TINT} />
      <Path d="M70 106 L86 84 L100 98 L110 88 L130 106 Z" fill="#93C5FD" />
      <Circle cx={120} cy={78} r={6} fill={AMBER} />
      <Rect x={64} y={124} width={50} height={7} rx={3.5} fill={MUTED} />
      <Rect x={64} y={135} width={34} height={7} rx={3.5} fill={BORDER} />

      <Path
        d="M140 130 C148 130 154 136 154 144 C154 154 140 168 140 168 C140 168 126 154 126 144 C126 136 132 130 140 130 Z"
        fill={BRAND}
      />
      <Circle cx={140} cy={144} r={5} fill="#FFFFFF" />

      <Circle cx={155} cy={58} r={16} fill={BRAND} />
      <Rect x={147} y={52} width={16} height={12} rx={3} fill="#FFFFFF" />
      <Circle cx={155} cy={58} r={3.5} fill={BRAND} />
    </Svg>
  );
}

export function TrackIllustration() {
  return (
    <Svg width={200} height={200} viewBox="0 0 200 200">
      <Circle cx={100} cy={100} r={90} fill={BRAND_TINT} />

      <Rect x={45} y={50} width={110} height={110} rx={16} fill="#FFFFFF" stroke={BORDER} strokeWidth={1.5} />

      <Path d="M70 75 L70 145" stroke={BORDER} strokeWidth={3} strokeLinecap="round" />
      <Path d="M70 75 L70 112" stroke={BRAND} strokeWidth={3} strokeLinecap="round" />

      <Circle cx={70} cy={75} r={9} fill={BRAND} />
      <Path d="M65 75 L69 79 L76 71" stroke="#FFFFFF" strokeWidth={2.5} strokeLinecap="round" strokeLinejoin="round" fill="none" />
      <Circle cx={70} cy={112} r={9} fill={BRAND} />
      <Circle cx={70} cy={145} r={9} fill="#FFFFFF" stroke={MUTED} strokeWidth={2.5} />

      <Rect x={90} y={70} width={55} height={8} rx={4} fill={INK} />
      <Rect x={90} y={107} width={45} height={8} rx={4} fill={BRAND} />
      <Rect x={90} y={140} width={40} height={8} rx={4} fill={BORDER} />

      <Circle cx={155} cy={55} r={15} fill={AMBER} />
      <Circle cx={155} cy={50} r={4} fill="#FFFFFF" />
      <Path d="M147 62 C147 57 151 54 155 54 C159 54 163 57 163 62 Z" fill="#FFFFFF" />
    </Svg>
  );
}

export function PrivacyIllustration() {
  return (
    <Svg width={200} height={200} viewBox="0 0 200 200">
      <Circle cx={100} cy={100} r={90} fill={BRAND_TINT} />

      <Path
        d="M100 40 L140 55 L140 98 C140 128 120 148 100 158 C80 148 60 128 60 98 L60 55 Z"
        fill={BRAND}
      />
      <Path d="M82 100 L95 113 L120 85" stroke="#FFFFFF" strokeWidth={8} strokeLinecap="round" strokeLinejoin="round" fill="none" />

      <Circle cx={150} cy={140} r={17} fill="#FFFFFF" stroke={BORDER} strokeWidth={1.5} />
      <Path d="M140 140 C143 134 148 131 150 131 C152 131 157 134 160 140 C157 146 152 149 150 149 C148 149 143 146 140 140 Z" fill="none" stroke={INK} strokeWidth={2} />
      <Circle cx={150} cy={140} r={3} fill={INK} />
      <Path d="M137 128 L163 152" stroke={INK} strokeWidth={2} strokeLinecap="round" />
    </Svg>
  );
}
