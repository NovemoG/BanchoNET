export type AuthUser = {
  avatar_url?: string | null;
  country_code?: string | null;
  id: number;
  is_supporter?: boolean;
  session_verification_method?: string | null;
  session_verified?: boolean;
  username: string;
  [key: string]: unknown;
};

export type AuthSession = {
  accessToken: string;
  demoSessionCode?: string | null;
  expiresAt: number;
  refreshToken: string;
  scope?: string | null;
  tokenType: string;
  user: AuthUser;
};

export type ClientSession = {
  expires: string;
  user: AuthUser;
};

export type OAuthTokenResponse = {
  access_token: string;
  demo_session_code?: string | null;
  expires_in: number;
  refresh_token: string;
  scope?: string | null;
  token_type: string;
};

export type AuthActionState = {
  fieldErrors?: {
    email?: string;
    password?: string;
    username?: string;
  };
  message?: string;
  status: "idle" | "error";
};
