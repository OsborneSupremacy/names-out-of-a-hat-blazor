import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { SignIn } from './SignIn'
import { AddParticipantModal } from './AddParticipantModal'

describe('WhyEmailModal', () => {
  describe('on the sign-in page', () => {
    it('is offered but stays out of the way until asked for', async () => {
      const user = userEvent.setup()

      render(<SignIn />)

      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()

      await user.click(screen.getByRole('button', { name: 'Why do you need my email?' }))

      expect(screen.getByRole('dialog', { name: 'Why do we need your email?' })).toBeInTheDocument()
    })

    it('answers about the organizer’s own address', async () => {
      const user = userEvent.setup()

      render(<SignIn />)
      await user.click(screen.getByRole('button', { name: 'Why do you need my email?' }))

      expect(screen.getByRole('heading', { name: 'It’s how we know who you are' })).toBeInTheDocument()
      expect(screen.getByRole('heading', { name: 'It’s also your return address' })).toBeInTheDocument()

      // The answer people are really after, so it is on both versions.
      expect(screen.getByRole('heading', { name: 'Why not a phone number?' })).toBeInTheDocument()
    })

    // A button inside the sign-in form, so the wrong type attribute would submit the form and fire
    // off a magic link instead of explaining anything.
    it('does not submit the sign-in form', async () => {
      const user = userEvent.setup()

      render(<SignIn />)
      await user.click(screen.getByRole('button', { name: 'Why do you need my email?' }))

      expect(screen.queryByText('Please enter your email address')).not.toBeInTheDocument()
    })

    it('closes again', async () => {
      const user = userEvent.setup()

      render(<SignIn />)
      await user.click(screen.getByRole('button', { name: 'Why do you need my email?' }))
      await user.click(screen.getByRole('button', { name: 'Got it' }))

      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    })
  })

  describe('when adding a participant', () => {
    const renderModal = () =>
      render(<AddParticipantModal onClose={() => {}} onSubmit={async () => {}} />)

    it('answers about the participant’s address instead', async () => {
      const user = userEvent.setup()

      renderModal()
      await user.click(screen.getByRole('button', { name: 'Why do you need their email?' }))

      expect(
        screen.getByRole('dialog', { name: 'Why do we need their email?' })
      ).toBeInTheDocument()
      expect(
        screen.getByRole('heading', { name: 'It’s how we tell them whose name they drew' })
      ).toBeInTheDocument()
      expect(screen.getByRole('heading', { name: 'Why not a phone number?' })).toBeInTheDocument()

      // The sign-in answer must not leak into this one.
      expect(
        screen.queryByRole('heading', { name: 'It’s how we know who you are' })
      ).not.toBeInTheDocument()
    })

    // It opens on top of a dialog that is itself mid-form, so closing it must leave the half-filled
    // participant behind rather than taking the form down with it.
    it('leaves the participant form open and filled in underneath', async () => {
      const user = userEvent.setup()

      renderModal()
      await user.type(screen.getByLabelText('Name *'), 'Alice')

      await user.click(screen.getByRole('button', { name: 'Why do you need their email?' }))
      await user.click(screen.getByRole('button', { name: 'Got it' }))

      expect(screen.getByRole('button', { name: 'Add Participant' })).toBeInTheDocument()
      expect(screen.getByLabelText('Name *')).toHaveValue('Alice')
    })

    it('does not submit the participant form', async () => {
      const user = userEvent.setup()

      renderModal()
      await user.click(screen.getByRole('button', { name: 'Why do you need their email?' }))

      expect(screen.queryByText('Name cannot be empty')).not.toBeInTheDocument()
    })
  })
})
