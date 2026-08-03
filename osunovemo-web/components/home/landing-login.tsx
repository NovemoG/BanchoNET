"use client";

import type {FormEvent} from "react";
import {useId, useState} from "react";
import {Download} from "lucide-react";
import {Field, FieldError, FieldGroup, FieldLabel} from "@/components/ui/field";
import {register, signIn} from "@/lib/auth/client";
import type {AuthActionState} from "@/lib/auth/types";
import {cn} from "@/lib/utils";

export function LandingLogin() {
    const [isOpen, setIsOpen] = useState(false);
    const [isPending, setIsPending] = useState(false);
    const [mode, setMode] = useState<"register" | "signin">("signin");
    const [state, setState] = useState<AuthActionState>({status: "idle"});
    const usernameId = useId();
    const passwordId = useId();
    const emailId = useId();

    const switchMode = (next: "register" | "signin") => {
        if (next === mode) {
            return;
        }

        setMode(next);
        setState({status: "idle"});
    };

    const handleRegister = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();

        const formData = new FormData(event.currentTarget);
        const username = String(formData.get("username") ?? "").trim();
        const email = String(formData.get("email") ?? "").trim();
        const password = String(formData.get("password") ?? "");
        const fieldErrors: AuthActionState["fieldErrors"] = {};

        if (username.length === 0) {
            fieldErrors.username = "Username is required.";
        }

        if (email.length === 0) {
            fieldErrors.email = "Email is required.";
        }

        if (password.length === 0) {
            fieldErrors.password = "Password is required.";
        }

        if (Object.keys(fieldErrors).length > 0) {
            setState({fieldErrors, status: "error"});
            return;
        }

        setIsPending(true);
        const result = await register({email, password, username});

        if (result.ok) {
            // The API creates the account and we sign in with it in the same request, so there is
            // nothing else to do but land the user somewhere useful.
            window.location.assign("/");
            return;
        }

        setState({
            fieldErrors: {
                email: result.fieldErrors.user_email?.join(" "),
                password: result.fieldErrors.password?.join(" "),
                username: result.fieldErrors.username?.join(" "),
            },
            message: Object.keys(result.fieldErrors).length > 0 ? undefined : result.message ?? undefined,
            status: "error",
        });
        setIsPending(false);
    };

    const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();

        const formData = new FormData(event.currentTarget);
        const username = String(formData.get("username") ?? "").trim();
        const password = String(formData.get("password") ?? "");
        const fieldErrors: AuthActionState["fieldErrors"] = {};

        if (username.length === 0) {
            fieldErrors.username = "Username is required.";
        }

        if (password.length === 0) {
            fieldErrors.password = "Password is required.";
        }

        if (Object.keys(fieldErrors).length > 0) {
            setState({fieldErrors, status: "error"});
            return;
        }

        setIsPending(true);
        const result = await signIn("credentials", {
            callbackUrl: "/",
            password,
            redirect: false,
            username,
        });

        if (result.ok) {
            window.location.assign("/");
            return;
        }

        setState({message: result.error ?? "Sign in failed.", status: "error"});
        setIsPending(false);
    };

    return (
        <div className="relative">
            <button
                className="flex cursor-pointer items-center whitespace-nowrap text-sm font-semibold text-white lowercase transition-colors hover:text-osu-h1 focus:text-osu-h1 focus:outline-none"
                type="button"
                onClick={() => setIsOpen((value) => !value)}
            >
                sign in
            </button>

            <div
                className={cn(
                    "absolute right-0 top-full z-20 mt-4 w-[min(20rem,calc(100vw-2rem))] rounded-lg bg-osu-b4 p-4 text-left shadow-[0_12px_32px_rgba(0,0,0,0.45)] transition",
                    isOpen ? "visible translate-y-0 opacity-100" : "invisible -translate-y-1 opacity-0",
                )}
            >
                <div className="mb-4 flex gap-1 rounded-md bg-osu-b6 p-1">
                    {(["signin", "register"] as const).map((value) => (
                        <button
                            className={cn(
                                "flex-1 rounded px-3 py-1.5 text-xs font-semibold lowercase transition-colors",
                                mode === value ? "bg-osu-b3 text-white" : "text-osu-c1 hover:text-white",
                            )}
                            key={value}
                            onClick={() => switchMode(value)}
                            type="button"
                        >
                            {value === "signin" ? "sign in" : "register"}
                        </button>
                    ))}
                </div>

                <form
                    className="flex flex-col gap-4"
                    onSubmit={mode === "register" ? handleRegister : handleSubmit}
                >
                    <input name="callbackUrl" type="hidden" value="/"/>

                    <FieldGroup>
                        <Field data-invalid={state?.fieldErrors?.username != null}>
                            <FieldLabel htmlFor={usernameId}>username</FieldLabel>
                            <input
                                aria-invalid={state?.fieldErrors?.username != null}
                                autoComplete="username"
                                className="h-9 rounded-md border border-white/10 bg-osu-b6 px-3 text-sm text-white outline-none transition focus:border-osu-h1"
                                disabled={isPending}
                                id={usernameId}
                                name="username"
                                type="text"
                            />
                            <FieldError
                                errors={
                                    state?.fieldErrors?.username == null
                                        ? undefined
                                        : [{message: state.fieldErrors.username}]
                                }
                            />
                        </Field>

                        {mode === "register" ? (
                            <Field data-invalid={state?.fieldErrors?.email != null}>
                                <FieldLabel htmlFor={emailId}>email</FieldLabel>
                                <input
                                    aria-invalid={state?.fieldErrors?.email != null}
                                    autoComplete="email"
                                    className="h-9 rounded-md border border-white/10 bg-osu-b6 px-3 text-sm text-white outline-none transition focus:border-osu-h1"
                                    disabled={isPending}
                                    id={emailId}
                                    name="email"
                                    type="email"
                                />
                                <FieldError
                                    errors={
                                        state?.fieldErrors?.email == null
                                            ? undefined
                                            : [{message: state.fieldErrors.email}]
                                    }
                                />
                            </Field>
                        ) : null}

                        <Field data-invalid={state?.fieldErrors?.password != null}>
                            <FieldLabel htmlFor={passwordId}>password</FieldLabel>
                            <input
                                aria-invalid={state?.fieldErrors?.password != null}
                                autoComplete={mode === "register" ? "new-password" : "current-password"}
                                className="h-9 rounded-md border border-white/10 bg-osu-b6 px-3 text-sm text-white outline-none transition focus:border-osu-h1"
                                disabled={isPending}
                                id={passwordId}
                                name="password"
                                type="password"
                            />
                            <FieldError
                                errors={
                                    state?.fieldErrors?.password == null
                                        ? undefined
                                        : [{message: state.fieldErrors.password}]
                                }
                            />
                        </Field>
                    </FieldGroup>

                    {state?.message != null ? (
                        <p className="rounded-md bg-osu-red-3/20 px-3 py-2 text-sm text-osu-red-1">
                            {state.message}
                        </p>
                    ) : null}

                    <button
                        className="flex h-10 items-center justify-center gap-2 rounded-md bg-osu-h2 px-4 text-sm font-bold text-white transition hover:bg-osu-h1 disabled:cursor-default disabled:opacity-70"
                        disabled={isPending}
                        type="submit"
                    >
                        <Download aria-hidden className="size-4"/>
                        {mode === "register" ? "create account" : "sign in"}
                    </button>
                </form>
            </div>
        </div>
    );
}
