export const onboardingKeys = {
  root: ['onboarding'] as const,
  draft: () => [...onboardingKeys.root, 'draft'] as const,
  completion: () => [...onboardingKeys.root, 'completion'] as const,
}
