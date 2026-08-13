import { FormControl, FormGroup } from '@angular/forms';

import { collectFormFailures, matchingPasswords, passwordComplexity, required } from './validators';

describe('form validators', () => {
  it('accepts a 12-character complex password', () => {
    const control = new FormControl('Correct-Horse-1', { validators: passwordComplexity });
    expect(control.valid).toBe(true);
  });

  it('rejects a short password and lists the missing classes', () => {
    const control = new FormControl('short', { validators: passwordComplexity });
    expect(control.invalid).toBe(true);
    const problems = (control.getError('passwordComplexity') as { problems: string[] }).problems;
    expect(problems).toContain('at least 12 characters');
    expect(problems).toContain('an uppercase letter');
    expect(problems).toContain('a number');
    expect(problems).toContain('a special character');
  });

  it('lists every invalid signup field', () => {
    const form = new FormGroup(
      {
        email: new FormControl('', { validators: required }),
        username: new FormControl('', { validators: required }),
        password: new FormControl('Correct-Horse-1', { validators: required }),
        confirmPassword: new FormControl('other', { validators: required }),
      },
      { validators: matchingPasswords },
    );

    const failures = collectFormFailures(form, {
      email: 'Email',
      username: 'Username',
      password: 'Password',
      confirmPassword: 'Confirm password',
    });

    expect(failures).toContain('Email is not filled in.');
    expect(failures).toContain('Username is not filled in.');
    expect(failures).toContain('Confirm password does not match the password.');
  });
});
