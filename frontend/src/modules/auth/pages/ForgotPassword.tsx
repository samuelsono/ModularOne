import { useState } from "react";
import { Button, Divider, Field, Input, Link, MessageBar, MessageBarBody, Spinner } from "@fluentui/react-components";
import { ApiError } from '@platform/api/apiClient';
import * as authService from '@platform/api/authService';

function ForgotPassword() {
  const [email, setEmail] = useState("");
  const [emailSubmitted, setEmailSubmitted] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setIsSubmitting(true);

    try {
      await authService.forgotPassword({ email: email.trim() });
      setEmailSubmitted(true);
    } catch (submitError) {
      const message = submitError instanceof ApiError
        ? submitError.message
        : "Unable to process your request. Please try again.";
      setError(message);
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <form className="flex flex-col text-center gap-3" onSubmit={handleSubmit}>
      <div className="mb-3">
        <p className="text-lg font-bold">{emailSubmitted ? "Email Sent" : "Forgot Password"}</p>
        {!emailSubmitted && <p>Enter your email to reset your password</p>}
      </div>

      {error && (
        <MessageBar intent="error">
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      )}

      {!emailSubmitted && (
        <Field label="Email" required>
          <Input
            type="email"
            placeholder="Enter your email"
            value={email}
            onChange={(_, data) => setEmail(data.value)}
            disabled={isSubmitting}
            autoComplete="email"
          />
        </Field>
      )}

      {emailSubmitted && (
        <p>
          We have sent an email to your address with instructions to reset your password.
          <br /><br />
          If this account exists, you will receive an email shortly.
        </p>
      )}

      {emailSubmitted && (
        <div className="flex justify-between items-center gap-2">
          <div>Did not receive instructions?</div>
          <Link
            href="/auth/forgot-password"
            className="text-sm"
            onClick={(event) => {
              event.preventDefault();
              setEmailSubmitted(false);
              setError(null);
            }}
          >
            Resend Email
          </Link>
        </div>
      )}

      {!emailSubmitted && (
        <div className="flex justify-between items-center gap-2">
          <div>Don't want to reset your password?</div>
          <Link href="/auth/login" className="text-sm">Go to Login</Link>
        </div>
      )}

      {emailSubmitted ? (
        <div className="flex justify-between gap-2">
          <Button as="a" appearance="primary" className="w-full" href="/auth/login">Back to Login</Button>
        </div>
      ) : (
        <Button
          appearance="primary"
          className="w-full"
          type="submit"
          disabled={isSubmitting || !email.trim()}
          icon={isSubmitting ? <Spinner size="tiny" /> : undefined}
        >
          {isSubmitting ? "Sending..." : "Reset Password"}
        </Button>
      )}

      <Divider className="my-3">Or Login With</Divider>
      <div className="flex justify-between gap-2">
        <Button className="w-full" disabled>Google</Button>
        <Button className="w-full" disabled>Microsoft</Button>
      </div>
    </form>
  );
}

export default ForgotPassword;
